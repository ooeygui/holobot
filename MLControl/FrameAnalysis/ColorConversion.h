///-----------------------------------------------------------------
///   Class:          ColorConversion
///   Description:    Functions for Color Conversion
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#ifndef COLORCONVERSION_H
#define COLORCONVERSION_H

#include "Globals.h"

class ColorConversion {
public:
	static void ConvertRGBToYUV420();

	static void YUY2ToRGB24(int Y, int U, int V, BYTE *pR, BYTE *pG, BYTE *pB);

	static void YUY2ToRGB(UYVY_MACRO_PIXEL *YUY2InputFrame, RGBTRIPLELOCAL *RGB1, RGBTRIPLELOCAL *RGB2);

	static void convertFrameToRGB24();

	static void convertRGB24ToByteArray();

	static void convertRGBToRGBA(BYTE*, const BYTE*, const int);

};

#endif