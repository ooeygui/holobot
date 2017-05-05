///-----------------------------------------------------------------
///   Class:          PreProcessing
///   Description:    ProProcess raw RGB stream into a features to be consumed by CNTK
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#ifndef PREPROCESSING_H
#define PREPROCESSING_H

#include "Globals.h"

#include <opencv2\core.hpp>
#include <opencv2\imgproc.hpp>
#include <opencv2\features2d.hpp>

class PreProcessing {
public:
	static void ImageFrameProcessingLogic();

	static void resizeImage();
};

#endif