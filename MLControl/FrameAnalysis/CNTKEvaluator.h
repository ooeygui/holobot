///-----------------------------------------------------------------
///   Class:          CNTKEvaluator
///   Description:    CNTK Evaluation Functions
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#ifndef CNTKEVALUATOR_H
#define CNTKEVALUATOR_H

#include "Globals.h"
#include <sys/stat.h>
#include "Eval.h"
#include <string>
#include <regex>
// dependency files
#include "Helper.h"

using namespace Microsoft::MSR::CNTK;

class CNTKEvaluator {
public:
	static void GenerateEvaluationMode(std::string modelFilePath);

	static std::vector<float> getFeatures(uint32 reqWidth, uint32 reqHeight);

	static int findClass(std::vector<float> outputs);

	static int EvaluateImageClassificationModel();

	static Platform::String^ ClassifyImages();
};
#endif