#ifndef GLOBALS_H_
#define GLOBALS_H_

#include "pch.h"
#include <wrl\module.h>

/* Declare all the global variables shared between multiple project classes */

#define imageChannels 3
#define featureImageWidth 224
#define featureImageHeight 224


#define inputImageWidth 640
#define inputImageHeight 480
//#define inputImageWidth 960
//#define inputImageHeight 544

#define outputImageWidth 800
#define outputImageHeight 48

#define classificationFreq 30
#define classFrameAvailable 45

typedef unsigned char BYTE;
typedef unsigned long DWORD;

#define myMin(a,b) (a < b)? a : b;

inline BYTE clamp(int x) {
	BYTE minV = myMin(255, x);
	if (minV > 0) return minV;
	return 0;
}

// Helper structure defining the RGB format and byte positioning.
struct RGBTRIPLELOCAL
{
	BYTE R;
	BYTE G;
	BYTE B;
};

// Helper structure defining the YUV format and byte positioning.
struct YUVTRIPLE
{
	BYTE Y;
	BYTE U;
	BYTE V;
};

// A struct representing a single YUY2 format macropixel.  The macropixel describes
// two pixels in the final frame.  Each pixel has its own luma value (Y1 and Y2), but
// both share the same chroma (U and V).
struct UYVY_MACRO_PIXEL
{
	BYTE U;
	BYTE Y1;
	BYTE V;
	BYTE Y2;
};

extern UYVY_MACRO_PIXEL YUY2InputFrame[inputImageHeight][inputImageWidth / 2];

extern UYVY_MACRO_PIXEL YUY2OutputFrame[outputImageHeight][outputImageWidth];

extern BYTE resizedImagedataArray[featureImageWidth * featureImageHeight * imageChannels];

extern BYTE originalImagedataArray[inputImageHeight * inputImageWidth * imageChannels];

extern BYTE newOriginalImagedataArray[inputImageHeight * inputImageWidth * (imageChannels + 1)];

extern RGBTRIPLELOCAL RGBInputFrame[inputImageHeight][inputImageWidth];

extern RGBTRIPLELOCAL rgbBmpFrame[outputImageHeight][outputImageWidth];

extern YUVTRIPLE pYuv[outputImageHeight][outputImageWidth];

extern std::vector<float> features;

extern bool imageInjected;

extern bool modelGenStatus;

extern std::atomic_bool evaluating;

#endif