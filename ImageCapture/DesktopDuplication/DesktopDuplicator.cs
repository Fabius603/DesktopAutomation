using System;
using System.Drawing.Imaging;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX.Mathematics.Interop;
using ImageHelperMethods;

namespace ImageCapture.DesktopDuplication
{
    public class DesktopDuplicator : IDisposable
    {
        #region propertiers
        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;

        private Bitmap _currentCachedBitmap = null;
        private int _currentCachedBitmapWidth = 0;
        private int _currentCachedBitmapHeight = 0;

        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        private bool disposed = false;
        private readonly PointerInfo sharedPointerInfo = new PointerInfo();
        private int aquireFrameTimeout { get; set; } = 0;
        #endregion

        public DesktopDuplicator(int screenIdx)
        {
            Factory1 factory = null;
            Adapter1 adapter = null;
            Output output = null;
            Output1 output1 = null;
            bool success = false;

            var screens = ScreenHelper.GetScreens();

            if (screens == null || screenIdx < 0 || screenIdx >= screens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(screenIdx), "Invalid screen index specified.");
            }

            var idx = ScreenHelper.GetAdapterAndOutputIndex(screens[screenIdx]);
            if (idx == null)
            {
                throw new DesktopDuplicationException($"Could not find the specified graphics card adapter or output device for screen index {screenIdx}.");
            }
            mWhichOutputDevice = idx.Value.outputIdx;

            try
            {
                factory = new Factory1();
                adapter = factory.GetAdapter1(idx.Value.adapterIdx);
                if (adapter == null) throw new DesktopDuplicationException("Could not find the specified graphics card adapter (null).");

                mDevice = new Device(adapter, DeviceCreationFlags.None);

                output = adapter.GetOutput(idx.Value.outputIdx);
                if (output == null) throw new DesktopDuplicationException("Could not find the specified output device (null).");

                output1 = output.QueryInterface<Output1>();
                if (output1 == null) throw new DesktopDuplicationException("Could not query Output1 interface.");

                mOutputDesc = output.Description;
                mTextureDesc = new Texture2DDescription()
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm, // Use UNorm for 8-bit per channel
                    Width = GetWidth(mOutputDesc.DesktopBounds),
                    Height = GetHeight(mOutputDesc.DesktopBounds),
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging // Staging texture allows CPU read access
                };

                mDeskDupl = output1.DuplicateOutput(mDevice);
                success = true;
            }
            catch (SharpDXException ex)
            {
                string specificError = "Failed to initialize desktop duplication.";
                if (factory == null) specificError = "Failed to create DXGI Factory.";
                else if (adapter == null) specificError = "Could not find the specified graphics card adapter.";
                else if (mDevice == null) specificError = "Failed to create D3D11 Device.";
                else if (output == null) specificError = "Could not find the specified output device.";
                else if (output1 == null) specificError = "Could not query Output1 interface from output device.";
                else if (mDeskDupl == null && ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotCurrentlyAvailable.Result.Code)
                {
                    throw new DesktopDuplicationException("There is already the maximum number of applications using the Desktop Duplication API running, please close one of the applications and try again.", ex);
                }
                throw new DesktopDuplicationException(specificError, ex);
            }
            finally
            {
                output1?.Dispose();
                output?.Dispose();
                adapter?.Dispose();
                factory?.Dispose();

                if (!success)
                {
                    mDeskDupl?.Dispose();
                    mDevice?.Dispose();
                }
            }
        }

        #region public methods

        public void SetFrameTimeout(int timeout)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be non-negative.");
            aquireFrameTimeout = timeout;
        }

        public DesktopFrame GetLatestFrame()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            SharpDX.DXGI.Resource desktopResource = null;
            DesktopFrame frame = null;
            bool frameSuccessfullyAcquiredFromDxgi = false;

            try
            {
                bool retrievalTimedOut = RetrieveFrameInternal(out desktopResource);

                frameSuccessfullyAcquiredFromDxgi = !retrievalTimedOut;

                frame = new DesktopFrame();
                RetrieveFrameMetadata(frame);
                RetrieveCursorMetadata(frame);

                bool frameWasUpdated = (desktopResource != null || frameInfo.LastPresentTime > 0 || frameInfo.AccumulatedFrames > 0);

                if (frameWasUpdated)
                {
                    ProcessFrameIntoInternalBitmap();
                }

                if (_currentCachedBitmap != null)
                {
                    frame.DesktopImage = (Bitmap)_currentCachedBitmap.Clone();
                }
                else
                {
                    frame.DesktopImage = null;
                }

                return frame;
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code ||
                    ex.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code ||
                    ex.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
                {
                    Dispose();
                    throw new DesktopDuplicationException("Desktop Duplication session became invalid (Access Lost/Device Removed/Device Reset). Please recreate DesktopDuplicator.", ex);
                }
                frame?.DesktopImage?.Dispose();
                frame?.Dispose();
                throw new DesktopDuplicationException("Failed during frame processing or metadata retrieval.", ex);
            }
            finally
            {
                desktopResource?.Dispose();

                if (frameSuccessfullyAcquiredFromDxgi)
                {
                    try
                    {
                        mDeskDupl.ReleaseFrame();
                    }
                    catch (SharpDXException ex)
                    {
                        if (ex.ResultCode.Failure)
                        {
                            Debug.WriteLine($"Failed to release frame: {ex.Message}");
                        }
                    }
                }
            }
        }

        #endregion

        #region private methods
        private bool RetrieveFrameInternal(out SharpDX.DXGI.Resource desktopResourceOut)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            desktopResourceOut = null;

            // Ensure desktopImageTexture (the staging buffer) is correctly sized.
            // This might change if screen resolution changes.
            int currentOutputWidth = GetWidth(mOutputDesc.DesktopBounds);
            int currentOutputHeight = GetHeight(mOutputDesc.DesktopBounds);

            if (desktopImageTexture == null || mTextureDesc.Width != currentOutputWidth || mTextureDesc.Height != currentOutputHeight)
            {
                desktopImageTexture?.Dispose(); // Dispose old texture if size changed
                mTextureDesc.Width = currentOutputWidth;
                mTextureDesc.Height = currentOutputHeight;
                // Handle invalid dimensions defensively
                if (currentOutputWidth <= 0 || currentOutputHeight <= 0)
                {
                    throw new DesktopDuplicationException($"Invalid output dimensions: Width={currentOutputWidth}, Height={currentOutputHeight}");
                }
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);
            }

            // Re-initialize frameInfo struct for each attempt
            frameInfo = new OutputDuplicateFrameInformation();

            try
            {
                Result res = mDeskDupl.TryAcquireNextFrame(aquireFrameTimeout, out frameInfo, out desktopResourceOut); 
                if (res.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    desktopResourceOut?.Dispose();
                    desktopResourceOut = null;
                    return true; 
                }
                res.CheckError(); 
            }
            catch (SharpDXException ex)
            {
                desktopResourceOut?.Dispose();
                desktopResourceOut = null;
                if (ex.ResultCode.Failure)
                {
                    throw; 
                }
            }

            if (desktopResourceOut != null)
            {
                using (var tempTexture = desktopResourceOut.QueryInterface<Texture2D>())
                {
                    mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
                }
            }
            return false; 
        }


        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DesktopDuplicator));

            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                // Get moved regions
                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize / Marshal.SizeOf<OutputDuplicateMoveRectangle>() + 1];
                mDeskDupl.GetFrameMoveRects(movedRectangles.Length * Marshal.SizeOf<OutputDuplicateMoveRectangle>(), movedRectangles, out int movedRegionsLengthInBytes);

                int numMovedRects = movedRegionsLengthInBytes / Marshal.SizeOf<OutputDuplicateMoveRectangle>();
                frame.MovedRegions = new MovedRegion[numMovedRects];
                for (int i = 0; i < numMovedRects; i++)
                {
                    frame.MovedRegions[i] = new MovedRegion()
                    {
                        Source = new Point(movedRectangles[i].SourcePoint.X, movedRectangles[i].SourcePoint.Y),
                        Destination = ToRectangle(movedRectangles[i].DestinationRect)
                    };
                }

                // Get dirty regions
                RawRectangle[] dirtyRectangles = new RawRectangle[frameInfo.TotalMetadataBufferSize / Marshal.SizeOf<RawRectangle>() + 1];
                mDeskDupl.GetFrameDirtyRects(dirtyRectangles.Length * Marshal.SizeOf<RawRectangle>(), dirtyRectangles, out int dirtyRegionsLengthInBytes);

                int numDirtyRects = dirtyRegionsLengthInBytes / Marshal.SizeOf<RawRectangle>();
                frame.UpdatedRegions = new Rectangle[numDirtyRects];
                for (int i = 0; i < numDirtyRects; i++)
                {
                    frame.UpdatedRegions[i] = ToRectangle(dirtyRectangles[i]);
                }
            }
            else
            {
                frame.MovedRegions = Array.Empty<MovedRegion>();
                frame.UpdatedRegions = Array.Empty<Rectangle>();
            }
        }

        private void RetrieveCursorMetadata(DesktopFrame frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DesktopDuplicator));

            if (frameInfo.LastMouseUpdateTime == 0 && frameInfo.PointerShapeBufferSize == 0 && sharedPointerInfo.LastTimeStamp > 0)
            {
                frame.CursorVisible = sharedPointerInfo.Visible;
                frame.CursorLocation = new Point(sharedPointerInfo.Position.X, sharedPointerInfo.Position.Y);
                return;
            }
            if (frameInfo.LastMouseUpdateTime == 0 && frameInfo.PointerShapeBufferSize == 0) return;


            bool updatePosition = true;
            if (!frameInfo.PointerPosition.Visible && sharedPointerInfo.WhoUpdatedPositionLast != mWhichOutputDevice && sharedPointerInfo.Visible)
            {
                updatePosition = false;
            }

            if (frameInfo.PointerPosition.Visible && sharedPointerInfo.Visible &&
                sharedPointerInfo.WhoUpdatedPositionLast != mWhichOutputDevice &&
                sharedPointerInfo.LastTimeStamp > frameInfo.LastMouseUpdateTime)
            {
                updatePosition = false;
            }

            if (updatePosition)
            {
                sharedPointerInfo.Position = frameInfo.PointerPosition.Position;
                sharedPointerInfo.Visible = frameInfo.PointerPosition.Visible;
                sharedPointerInfo.WhoUpdatedPositionLast = mWhichOutputDevice;
                sharedPointerInfo.LastTimeStamp = frameInfo.LastMouseUpdateTime;
            }

            frame.CursorVisible = sharedPointerInfo.Visible;
            frame.CursorLocation = new Point(sharedPointerInfo.Position.X, sharedPointerInfo.Position.Y);

            if (frameInfo.PointerShapeBufferSize == 0)
                return;

            if (frameInfo.PointerShapeBufferSize > sharedPointerInfo.BufferSize)
            {
                sharedPointerInfo.PtrShapeBuffer = new byte[frameInfo.PointerShapeBufferSize];
                sharedPointerInfo.BufferSize = frameInfo.PointerShapeBufferSize;
            }

            try
            {
                unsafe
                {
                    fixed (byte* ptrShapeBufferPtr = sharedPointerInfo.PtrShapeBuffer)
                    {
                        mDeskDupl.GetFramePointerShape(sharedPointerInfo.BufferSize, (IntPtr)ptrShapeBufferPtr, out int requiredBufferSize, out sharedPointerInfo.ShapeInfo);
                    }
                }
                // frame.CursorShape = sharedPointerInfo.PtrShapeBuffer; // You had these commented out
                // frame.CursorShapeInfo = sharedPointerInfo.ShapeInfo; // If needed, uncomment and manage data ownership
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    throw new DesktopDuplicationException("Failed to get frame pointer shape.", ex);
                }
            }
        }

        private int GetWidth(RawRectangle rect) => rect.Right - rect.Left;
        private int GetHeight(RawRectangle rect) => rect.Bottom - rect.Top;

        /// <summary>
        /// Maps the desktopImageTexture (DXGI staging buffer) and copies its content into the
        /// internal _currentCachedBitmap. Recreates _currentCachedBitmap if resolution changes.
        /// </summary>
        private void ProcessFrameIntoInternalBitmap()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DesktopDuplicator));
            if (desktopImageTexture == null) return; // Should not happen if AcquireNextFrame was successful

            bool mapped = false;
            DataBox mapSource = default;

            int currentTextureWidth = mTextureDesc.Width;
            int currentTextureHeight = mTextureDesc.Height;

            if (currentTextureWidth <= 0 || currentTextureHeight <= 0) return; // Defensive check for invalid texture dimensions

            // Check if the internal _currentCachedBitmap needs to be created or resized
            if (_currentCachedBitmap == null || _currentCachedBitmap.Width != currentTextureWidth || _currentCachedBitmap.Height != currentTextureHeight)
            {
                _currentCachedBitmap?.Dispose(); // Dispose the old Bitmap if it exists and dimensions changed
                try
                {
                    _currentCachedBitmap = new Bitmap(currentTextureWidth, currentTextureHeight, PixelFormat.Format32bppRgb); // Match DXGI's B8G8R8A8_UNorm
                    _currentCachedBitmapWidth = currentTextureWidth;
                    _currentCachedBitmapHeight = currentTextureHeight;
                }
                catch (OutOfMemoryException oomEx)
                {
                    Debug.WriteLine($"DesktopDuplicator: OutOfMemoryException when creating _currentCachedBitmap: {oomEx.Message}");
                    _currentCachedBitmap = null; // Mark as null so GetLatestFrame returns null for image
                    throw; // Re-throw to caller to indicate severe memory issue
                }
            }

            // If _currentCachedBitmap is still null (e.g., OOM on creation), skip copy
            if (_currentCachedBitmap == null) return;

            // Now, copy the pixel data from the DXGI staging texture into _currentCachedBitmap
            try
            {
                mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);
                mapped = true;

                var boundsRect = new Rectangle(0, 0, _currentCachedBitmapWidth, _currentCachedBitmapHeight);
                BitmapData mapDest = null;

                try
                {
                    mapDest = _currentCachedBitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, _currentCachedBitmap.PixelFormat);
                    IntPtr sourcePtr = mapSource.DataPointer;
                    IntPtr destPtr = mapDest.Scan0;
                    int sourceRowPitch = mapSource.RowPitch;
                    int destStride = mapDest.Stride;
                    int bytesPerRowToCopy = _currentCachedBitmapWidth * 4; // B8G8R8A8_UNorm is 4 bytes per pixel

                    for (int y = 0; y < _currentCachedBitmapHeight; y++)
                    {
                        // Utilities.CopyMemory is usually faster than Marshal.Copy
                        Utilities.CopyMemory(destPtr, sourcePtr, bytesPerRowToCopy);
                        sourcePtr = IntPtr.Add(sourcePtr, sourceRowPitch);
                        destPtr = IntPtr.Add(destPtr, destStride);
                    }
                }
                finally
                {
                    if (mapDest != null)
                        _currentCachedBitmap.UnlockBits(mapDest);
                }
            }
            catch (SharpDXException ex)
            {
                Debug.WriteLine($"DesktopDuplicator: Error mapping or copying resource to internal bitmap: {ex.Message}");
                // In case of error, consider the cached bitmap invalid for this frame
                _currentCachedBitmap?.Dispose();
                _currentCachedBitmap = null;
                // Optionally re-throw or handle as a non-fatal error for the frame.
            }
            finally
            {
                if (mapped)
                {
                    mDevice.ImmediateContext.UnmapSubresource(desktopImageTexture, 0);
                }
            }
        }

        private Rectangle ToRectangle(RawRectangle r)
        {
            return new Rectangle(r.Left, r.Top, GetWidth(r), GetHeight(r));
        }

        #endregion

        #region dispose
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                mDeskDupl?.Dispose();
                mDeskDupl = null;

                desktopImageTexture?.Dispose(); // Dispose the internal staging buffer
                desktopImageTexture = null;

                _currentCachedBitmap?.Dispose(); // Dispose the internally managed Bitmap
                _currentCachedBitmap = null;

                mDevice?.Dispose();
                mDevice = null;
            }
            disposed = true;
        }

        ~DesktopDuplicator()
        {
            Dispose(false);
        }
        #endregion
    }
}