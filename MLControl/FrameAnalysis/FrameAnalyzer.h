#pragma once

namespace FrameAnalysis
{
    public ref class FrameAnalyzer sealed
    {
    public:
        FrameAnalyzer();
        int BeginProcessing(Windows::Graphics::Imaging::BitmapBuffer^ bitmapBuffer, int stride, int width, int height);
		Platform::String^ ClassificztionById(int id);
        void Init();
    private:
        int ProcessFrame(BYTE* frameBytes, UINT32 frameSize, int stride, int width, int height);
    };
}
