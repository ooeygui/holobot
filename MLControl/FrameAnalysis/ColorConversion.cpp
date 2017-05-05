///-----------------------------------------------------------------
///   Class:          ColorConversion
///   Description:    Functions for Color Conversion
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#include "ColorConversion.h"

void ColorConversion::ConvertRGBToYUV420()
{
	for (DWORD y = 0; y < outputImageHeight; y++)
	{
		for (DWORD x = 0; x < outputImageWidth; x++)
		{
			// store the RGB data in temporary variables since it will be modified
			short R = rgbBmpFrame[y][x].R;
			short G = rgbBmpFrame[y][x].G;
			short B = rgbBmpFrame[y][x].B;

			// use integer calculations to derive the Y, U, and V values for the 
			// YUV format of every single pixel.  This essentially converts the
			// data from 4:4:4 RGB to 4:4:4 YUV
			pYuv[y][x].Y = ((66 * R + 129 * G + 25 * B + 128) >> 8) + 16;    // Y
			pYuv[y][x].U = ((-38 * R - 74 * G + 112 * B + 128) >> 8) + 128;    // U
			pYuv[y][x].V = ((112 * R - 94 * G - 18 * B + 128) >> 8) + 128;   // V
		}
	}

	for (DWORD y = 0; y < outputImageHeight; y++)
	{
		for (DWORD x = 0; x < (outputImageWidth - 1); x += 2)
		{
			float uSum = 0;
			float vSum = 0;

			// add up the U and V portions of every pixel
			uSum = (float)(pYuv[y][x].U + pYuv[y][x + 1].U);
			vSum = (float)(pYuv[y][x].V + pYuv[y][x + 1].V);

			// Since a single chroma value for 4:2:2 format represents four pixels
			// at once (the same color is used for every four pixels) set the chroma
			// values of all of the pixels to the calculated average.
			pYuv[y][x].U = pYuv[y][x + 1].U = (char)(uSum / 2);
			pYuv[y][x].V = pYuv[y][x + 1].V = (char)(vSum / 2);
		}
	}
}

void ColorConversion::YUY2ToRGB24(int Y, int U, int V, BYTE *pR, BYTE *pG, BYTE *pB)
{
	int C = Y - 16;
	int D = U - 128;
	int E = V - 128;

	*pB = clamp(((298 * C + 409 * E + 128) >> 8));
	*pG = clamp(((298 * C - 100 * D - 208 * E + 128) >> 8));
	*pR = clamp(((298 * C + 516 * D + 128) >> 8));
}


void ColorConversion::YUY2ToRGB(UYVY_MACRO_PIXEL *YUY2InputFrame, RGBTRIPLELOCAL *RGB1, RGBTRIPLELOCAL *RGB2)
{
	BYTE  u, y1, y2, v;
	y1 = YUY2InputFrame->Y1;
	u = YUY2InputFrame->U;
	y2 = YUY2InputFrame->Y2;
	v = YUY2InputFrame->V;

	YUY2ToRGB24(y1, u, v, &(RGB1->B), &(RGB1->G), &(RGB1->R));
	YUY2ToRGB24(y2, u, v, &(RGB2->B), &(RGB2->G), &(RGB2->R));
}


void ColorConversion::convertFrameToRGB24()
{
	for (int i = 0; i < inputImageHeight; i++) {
		for (int j = 0; j < inputImageWidth; j = j + 2) {
			YUY2ToRGB(&YUY2InputFrame[i][j / 2], &RGBInputFrame[i][j], &RGBInputFrame[i][j + 1]);
		}
	}
}

void ColorConversion::convertRGB24ToByteArray()
{
	for (int y = 0; y < inputImageHeight; y++) {
		for (int x = 0; x < inputImageWidth; x++) {
			originalImagedataArray[y * inputImageWidth * 3 + 3 * x + 0] = RGBInputFrame[y][x].B;
			originalImagedataArray[y * inputImageWidth * 3 + 3 * x + 1] = RGBInputFrame[y][x].G;
			originalImagedataArray[y * inputImageWidth * 3 + 3 * x + 2] = RGBInputFrame[y][x].R;
		}
	}
}

void ColorConversion::convertRGBToRGBA(BYTE* rgba, const BYTE* rgb, const int count) {
	if (count == 0)
		return;
	for (int i = count; --i; rgba += 4, rgb += 3) {
		*(uint32_t*)(void*)rgba = *(const uint32_t*)(const void*)rgb;
	}
	for (int j = 0; j<3; ++j) {
		rgba[j] = rgb[j];
	}
}