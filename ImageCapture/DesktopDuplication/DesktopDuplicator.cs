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

namespace ImageCapture.DesktopDuplication
{
    

    public class DesktopDuplicator : IDisposable
    {
        private Device mDevice;
        private Texture2DDescription mTextureDesc;
        private OutputDescription mOutputDesc;
        private OutputDuplication mDeskDupl;

        private Texture2D desktopImageTexture = null;
        private OutputDuplicateFrameInformation frameInfo = new OutputDuplicateFrameInformation();
        private int mWhichOutputDevice = -1;

        private Bitmap finalImage1, finalImage2;
        private bool isFinalImage1 = false;
        private Bitmap FinalImage
        {
            get
            {
                return isFinalImage1 ? finalImage1 : finalImage2;
            }
            set
            {
                if (isFinalImage1)
                {
                    finalImage2?.Dispose();
                    finalImage2 = value;
                    finalImage1?.Dispose(); 
                    finalImage1 = null;
                }
                else
                {
                    finalImage1?.Dispose();
                    finalImage1 = value;
                    finalImage2?.Dispose();
                    finalImage2 = null;
                }
                isFinalImage1 = !isFinalImage1;
            }
        }

        private bool disposed = false;
        private readonly PointerInfo sharedPointerInfo = new PointerInfo();


        public DesktopDuplicator(int whichMonitor)
            : this(0, whichMonitor) { }

        public DesktopDuplicator(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            mWhichOutputDevice = whichOutputDevice;
            Factory1 factory = null;
            Adapter1 adapter = null;
            Output output = null;
            Output1 output1 = null;
            bool success = false;

            try
            {
                factory = new Factory1();
                adapter = factory.GetAdapter1(whichGraphicsCardAdapter);
                if (adapter == null) throw new DesktopDuplicationException("Could not find the specified graphics card adapter (null).");

                mDevice = new Device(adapter, DeviceCreationFlags.None);

                output = adapter.GetOutput(whichOutputDevice);
                if (output == null) throw new DesktopDuplicationException("Could not find the specified output device (null).");

                output1 = output.QueryInterface<Output1>();
                if (output1 == null) throw new DesktopDuplicationException("Could not query Output1 interface.");

                mOutputDesc = output.Description;
                mTextureDesc = new Texture2DDescription()
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = GetWidth(mOutputDesc.DesktopBounds),
                    Height = GetHeight(mOutputDesc.DesktopBounds),
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging
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
                // Dispose temporary COM objects used for initialization
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

        public DesktopFrame GetLatestFrame()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            bool frameAcquired = false;
            SharpDX.DXGI.Resource desktopResource = null; // Must be explicitly managed

            try
            {
                bool retrievalTimedOut = RetrieveFrameInternal(out desktopResource);

                if (retrievalTimedOut)
                    return null; // Timeout means no frame acquired this attempt

                frameAcquired = true;

                var frame = new DesktopFrame();
                RetrieveFrameMetadata(frame);
                RetrieveCursorMetadata(frame); 

                // Only process the image if there was an actual desktop update
                if (desktopResource != null || (frameInfo.LastPresentTime > 0 && frameInfo.AccumulatedFrames > 0)) // desktopResource non-null means new image.
                                                                                                                   // AccumulatedFrames > 0 can also indicate changes even if desktopResource is null (e.g. metadata only)
                {
                    if (desktopImageTexture != null && (desktopResource != null || frame.UpdatedRegions.Length > 0 || frame.MovedRegions.Length > 0))
                    {
                        ProcessFrame(frame);
                    }
                    else if (FinalImage != null)
                    { // If no new image but an old one exists
                        frame.DesktopImage = FinalImage; // Return the last known good image
                    }
                }
                else if (FinalImage != null) // No updates at all, but we have a previous image
                {
                    frame.DesktopImage = FinalImage;
                }


                return frame;
            }
            catch (SharpDXException ex)
            {
                // Handle specific errors that might invalidate the duplicator
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.AccessLost.Result.Code ||
                    ex.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceRemoved.Result.Code ||
                    ex.ResultCode.Code == SharpDX.DXGI.ResultCode.DeviceReset.Result.Code)
                {
                    Dispose();
                    throw new DesktopDuplicationException("Desktop Duplication session became invalid (Access Lost/Device Removed/Device Reset). Please recreate DesktopDuplicator.", ex);
                }
                throw new DesktopDuplicationException("Failed during frame processing or metadata retrieval.", ex);
            }
            finally
            {
                desktopResource?.Dispose(); 

                if (frameAcquired)
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

        private bool RetrieveFrameInternal(out SharpDX.DXGI.Resource desktopResourceOut)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DesktopDuplicator));

            desktopResourceOut = null;

            if (desktopImageTexture == null)
            {
                // Ensure width/height are positive before creating texture
                int width = GetWidth(mOutputDesc.DesktopBounds);
                int height = GetHeight(mOutputDesc.DesktopBounds);
                if (width <= 0 || height <= 0)
                {
                    throw new DesktopDuplicationException("Invalid dimensions for texture creation.");
                }
                mTextureDesc.Width = width;
                mTextureDesc.Height = height;
                desktopImageTexture = new Texture2D(mDevice, mTextureDesc);
            }

            // Re-initialize frameInfo struct for each attempt
            frameInfo = new OutputDuplicateFrameInformation();

            try
            {
                Result res = mDeskDupl.TryAcquireNextFrame(500, out frameInfo, out desktopResourceOut);
                if (res.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
                {
                    desktopResourceOut?.Dispose();
                    desktopResourceOut = null;
                    return true; // Timed out
                }
                res.CheckError(); // Throws on other failures
            }
            catch (SharpDXException ex)
            {
                desktopResourceOut?.Dispose();
                desktopResourceOut = null;

                if (ex.ResultCode.Failure)
                {
                    throw; // Rethrow to be handled by GetLatestFrame or signal failure
                }
            }

            if (desktopResourceOut != null)
            {
                using (var tempTexture = desktopResourceOut.QueryInterface<Texture2D>())
                {
                    mDevice.ImmediateContext.CopyResource(tempTexture, desktopImageTexture);
                }
            }
            return false; // Did not time out
        }


        private void RetrieveFrameMetadata(DesktopFrame frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DesktopDuplicator));

            if (frameInfo.TotalMetadataBufferSize > 0)
            {
                // Get moved regions
                OutputDuplicateMoveRectangle[] movedRectangles = new OutputDuplicateMoveRectangle[frameInfo.TotalMetadataBufferSize / Marshal.SizeOf<OutputDuplicateMoveRectangle>() + 1]; // Ensure enough space
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
                RawRectangle[] dirtyRectangles = new RawRectangle[frameInfo.TotalMetadataBufferSize / Marshal.SizeOf<RawRectangle>() + 1]; // Ensure enough space
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

            // A non-zero mouse update timestamp indicates that there is a mouse position update and optionally a shape change
            if (frameInfo.LastMouseUpdateTime == 0 && frameInfo.PointerShapeBufferSize == 0 && sharedPointerInfo.LastTimeStamp > 0)
            {
                // No new mouse update in this frame, but we have previous data
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
                return; // No new shape

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
                // frame.CursorShape = sharedPointerInfo.PtrShapeBuffer;
                // frame.CursorShapeInfo = sharedPointerInfo.ShapeInfo;
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

        private void ProcessFrame(DesktopFrame frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(DesktopDuplicator));
            if (desktopImageTexture == null) return;

            bool mapped = false;
            DataBox mapSource = default;

            try
            {
                mapSource = mDevice.ImmediateContext.MapSubresource(desktopImageTexture, 0, MapMode.Read, MapFlags.None);
                mapped = true;

                int width = GetWidth(mOutputDesc.DesktopBounds);
                int height = GetHeight(mOutputDesc.DesktopBounds);

                if (width <= 0 || height <= 0) return; // Invalid dimensions

                Bitmap currentFrameBitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                var boundsRect = new Rectangle(0, 0, width, height);
                BitmapData mapDest = null;

                try
                {
                    mapDest = currentFrameBitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, currentFrameBitmap.PixelFormat);
                    IntPtr sourcePtr = mapSource.DataPointer;
                    IntPtr destPtr = mapDest.Scan0;
                    int sourceRowPitch = mapSource.RowPitch;
                    int destStride = mapDest.Stride;
                    int bytesPerRowToCopy = width * 4; // Format is B8G8R8A8_UNorm (4 bytes per pixel)

                    for (int y = 0; y < height; y++)
                    {
                        Utilities.CopyMemory(destPtr, sourcePtr, bytesPerRowToCopy);
                        sourcePtr = IntPtr.Add(sourcePtr, sourceRowPitch);
                        destPtr = IntPtr.Add(destPtr, destStride);
                    }
                }
                finally
                {
                    if (mapDest != null)
                        currentFrameBitmap.UnlockBits(mapDest);
                }

                FinalImage = currentFrameBitmap;
                frame.DesktopImage = FinalImage;
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

        private void ReleaseDxgiFrame()
        {
            try
            {
                mDeskDupl.ReleaseFrame();
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Failure)
                {
                    Debug.WriteLine($"DesktopDuplication: Error releasing DXGI frame: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                mDeskDupl?.Dispose();
                mDeskDupl = null;

                desktopImageTexture?.Dispose();
                desktopImageTexture = null;

                finalImage1?.Dispose();
                finalImage1 = null;
                finalImage2?.Dispose();
                finalImage2 = null;

                mDevice?.Dispose();
                mDevice = null;
            }
            disposed = true;
        }

        ~DesktopDuplicator()
        {
            Dispose(false);
        }
    }
}