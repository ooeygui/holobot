#pragma once

namespace FrameAnalysis
{
    public ref class FrameAnalyzer sealed
    {
    public:
        FrameAnalyzer();
        Platform::String^ BeginProcessing(Windows::Graphics::Imaging::BitmapBuffer^ bitmapBuffer, int stride, int width, int height);
        void Init();
    private:
        Platform::String^ ProcessFrame(BYTE* frameBytes, UINT32 frameSize, int stride, int width, int height);
    };
}
