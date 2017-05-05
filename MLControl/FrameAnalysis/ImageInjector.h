///-----------------------------------------------------------------
///   Class:          ImageInjector
///   Description:    Functions to Inject BitMap Image into the output stream
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#ifndef IMAGEINJECTOR_H
#define IMAGEINJECTOR_H

#include "Globals.h"
#include "pch.h"

using namespace Windows::System;
using namespace Windows::Storage;
using namespace Windows::Storage::Streams;

using namespace concurrency;
using namespace Platform;
using namespace Windows::Foundation::Collections;

class ImageInjector {
public:
	static void DrawBitmap(UINT32 m_imageWidthInPixels, UINT32 m_imageHeightInPixels);

	static void ReadBmpFile(Platform::String^ bmpfilepath);

	static void WriteBmpFile();

	static void WriteTextFile(String^ ImageString);

	static void OverlayOutputOnVideoFrame(Platform::String^ ImageClassStrPath, uint32 m_imageWidthInPixels, uint32 m_imageHeightInPixels);
};

#endif