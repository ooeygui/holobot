#include "pch.h"
#include "FrameAnalyzer.h"
#include <wrl\module.h>
#include "VideoBufferLock.h"

#include "CNTKEvaluator.h"
#include "PreProcessing.h"
#include "ImageInjector.h"
#include "ColorConversion.h"

// For IMemoryBufferByteAccess
#include <MemoryBuffer.h>

using namespace FrameAnalysis;
using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Foundation;

FrameAnalyzer::FrameAnalyzer()
{
}
void PlatformStringToChar(Platform::String^ str, std::string& buffer)
{
    // Calculate the needed buffer size
    DWORD bufferSize = WideCharToMultiByte(CP_UTF8,
        0,
        str->Data(),
        str->Length(),
        nullptr,
        0,
        nullptr,
        nullptr);

    if (bufferSize == 0)
    {
        throw ref new ::Platform::Exception(GetLastError(), L"Failed to convert Platform string to utf8 string");
    }
    buffer.resize(bufferSize);
    /*buffer[bufferSize] = '\0';*/
    // Do the actual conversion
    WideCharToMultiByte(CP_UTF8,
        0,
        str->Data(),
        str->Length(),
        &buffer[0],
        bufferSize,
        nullptr,
        nullptr);
}

void ConsumeImage(
    _In_reads_(_Inexpressible_(lSrcStride * dwHeightInPixels)) const BYTE *pSrc,
    _In_ LONG lSrcStride,
    _In_ DWORD dwWidthInPixels,
    _In_ DWORD dwHeightInPixels)
{
    DWORD y = 0;
    // Lines within the destination rectangle.
    for (y = 0; y < dwHeightInPixels; y++)
    {
        const WORD *pSrc_Pixel = reinterpret_cast<const WORD*>(pSrc);

        for (DWORD x = 0; x < dwWidthInPixels; x += 2)
        {
            // Byte order is Y1 U Y2 V
            // Each WORD is a byte pair (Y, U/V)
            // Windows is little-endian so the order appears reversed.
            DWORD tmp = *reinterpret_cast<const DWORD*>(&pSrc_Pixel[x]);
            YUY2InputFrame[y][x / 2].Y1 = *((BYTE*)&tmp + 0);
            YUY2InputFrame[y][x / 2].U = *((BYTE*)&tmp + 1);
            YUY2InputFrame[y][x / 2].Y2 = *((BYTE*)&tmp + 2);
            YUY2InputFrame[y][x / 2].V = *((BYTE*)&tmp + 3);
        }
        pSrc += lSrcStride;
    }
}

void FrameAnalyzer::Init()
{
    if (modelGenStatus == false) {
        std::string ModelPathStr;
        PlatformStringToChar(Windows::ApplicationModel::Package::Current->InstalledLocation->Path, ModelPathStr);

        // Model to use
        ModelPathStr.append("\\ResNet.model");

        // Generate CNKT Model durint the first pass
        CNTKEvaluator::GenerateEvaluationMode(ModelPathStr);

        // Set Model generated status flag
        modelGenStatus = true;

        evaluating.store(false);
    }
}


IAsyncOperation<int>^  FrameAnalyzer::BeginProcessing(BitmapBuffer^ bitmapBuffer, int stride, int width, int height)
{
    return create_async([this, bitmapBuffer, stride, width, height]() -> int
    {

        auto memoryBufferReference = bitmapBuffer->CreateReference();

        ComPtr<IMemoryBufferByteAccess> pByteAccess;
        HRESULT hr = reinterpret_cast<IInspectable*>(memoryBufferReference)->QueryInterface(IID_PPV_ARGS(pByteAccess.GetAddressOf()));
        if (FAILED(hr))
        {
            return -1;
        }
        BYTE* pBuffer = nullptr;
        UINT32 capacity = 0;
        hr = pByteAccess->GetBuffer(&pBuffer, &capacity);
        if (FAILED(hr))
        {
            return -1;
        }

        return ProcessFrame(pBuffer, capacity, stride, width, height);
    });
}

int FrameAnalyzer::ProcessFrame(BYTE* frameBytes, UINT32 frameSize, int stride, int width, int height)
{
    // Consume the input YUY2 frame
    ConsumeImage(frameBytes, stride, width, height);

    // Image Pre-Processing Logic
    PreProcessing::ImageFrameProcessingLogic();

    if (evaluating.load() == false) {
        // CNTK Evaluator Logic (atomic)
        evaluating.store(true);
        //ImageInjector::WriteBmpFile();
        //String^ ImageClassStrPath = CNTKEvaluator::ClassifyImages();
        int classifiedImage = CNTKEvaluator::EvaluateImageClassificationModel();
        evaluating.store(false);
        return classifiedImage;
    }

    return -1;
}

Platform::String^ FrameAnalyzer::ClassificztionById(int id)
{
	std::string ImageClassStr = Helper::MapImageClassNumberToImageClassString(id);
	Platform::String^ ImageClassStrPath = Helper::stringToPlatformString(ImageClassStr);

	return ImageClassStrPath;
}
