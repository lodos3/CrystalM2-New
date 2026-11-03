namespace Client.MirGraphics
{
    internal enum D3D9VertexProcessing
    {
        Hardware,
        Software
    }

    // The client uses Device.PixelShader getters and other state reads, which
    // PureDevice forbids. Shader-model capability should never force REF CPU
    // rasterization: select the hardware device with software vertex processing
    // where hardware transformation is unavailable.
    internal static class D3D9DevicePolicy
    {
        internal const bool DeviceIsHardware = true;
        internal const bool EnablePureDevice = false;

        internal static D3D9VertexProcessing VertexProcessing(bool supportsHardwareTransform)
        {
            return supportsHardwareTransform ? D3D9VertexProcessing.Hardware : D3D9VertexProcessing.Software;
        }
    }
}