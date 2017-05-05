///-----------------------------------------------------------------
///   Class:          PreProcessing
///   Description:    ProProcess raw RGB stream into a features to be consumed by CNTK
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#include "PreProcessing.h"

// dependency files
#include "CNTKEvaluator.h"
#include "ColorConversion.h"
//#include "OpenCLKernelExecutor.h"

void PreProcessing::ImageFrameProcessingLogic()
{
	// convert input frame to RGB24 format
	ColorConversion::convertFrameToRGB24();

	// convert RGB frame to byte array
	ColorConversion::convertRGB24ToByteArray();

	// Call some OpenCL Kernel Call
	//cl_tmain();

	// Resize Image using openCV
	resizeImage();

	// get image features
	features = CNTKEvaluator::getFeatures(featureImageHeight, featureImageWidth);
}

void PreProcessing::resizeImage()
{
	cv::Mat Img = cv::Mat(inputImageHeight, inputImageWidth, CV_8UC3, originalImagedataArray);
	cv::Mat resImg = cv::Mat(featureImageHeight, featureImageWidth, CV_8UC3, cv::Scalar::all(0));
	cv::resize(Img, resImg, resImg.size(), 0, 0, CV_INTER_CUBIC);
	std::memcpy(resizedImagedataArray, resImg.data, 224 * 224 * 3 * sizeof(BYTE));
}
