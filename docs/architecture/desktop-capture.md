# Desktop capture

`DesktopCaptureService` owns the capture policy and serialized sessions keyed by Windows
output device name. `IDesktopDuplicationSessionFactory` resolves monitors and creates the
technical DXGI adapter; tests substitute this boundary and the service's `TimeProvider`.
The WPF monitor picker fills the identity companion field declared by the step descriptor.
An empty identity retains positional index selection for older jobs. A selected identity
must still be connected; it never silently resolves to a different monitor. Output names
identify Windows outputs, not permanent physical hardware identities across reinstalls.

Capture requests select either the latest available image or a new desktop presentation.
The time limit defaults to 250 ms and includes monitor resolution, queueing and retries.
Native device initialization, GPU readback and bitmap copying cannot be forcibly interrupted;
the limit bounds waiting and retry decisions, not hard real-time completion. Cancellation is
checked after acquisition and before ownership transfer. Native calls execute off the UI thread.
Existing jobs retain new-image preference and cached fallback. Strict new-image requests
fail with a timeout when no new presentation is available. Pointer-only updates do not
advance image provenance. There is no fixed startup sleep.

`DesktopDuplicator.UpdateFrame` updates the internal cache without cloning it. `CopyFrame`
creates one independently owned bitmap for the returned result, rotates it to desktop
orientation, and optionally composites the DXGI cursor shape. Color, masked-color and
monochrome shapes use the position from the same acquisition. DXGI positions already refer
to the shape's top left; no hotspot subtraction applies. Software-rendered cursors can
already be present in the desktop surface even when cursor composition is disabled.

Image bounds and offsets come from the producing session. Bounds changes recreate the
session; DXGI access loss and device failures trigger at most two recovery attempts within
the remaining time budget. Failed sessions cannot provide cached fallback images.
Acquired resources are released even when readback fails; session disposal occurs after
frame release. Application disposal cancels requests and waits for active acquisitions
before disposing sessions and their synchronization objects.

Deterministic tests cover policy, provenance, cancellation, recovery, ownership, geometry,
cursor masks, rotation, defaults and persisted jobs. Real GPU access loss, display
hotplug, software cursors and driver-specific rotation require Windows hardware testing.
