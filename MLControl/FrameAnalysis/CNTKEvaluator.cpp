///-----------------------------------------------------------------
///   Class:          CNTKEvaluator
///   Description:    CNTK Evaluation Functions
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#include "CNTKEvaluator.h"


template<typename ElemType>
using GetEvalProc = void(*)(IEvaluateModel<ElemType>**);
typedef std::pair<std::wstring, std::vector<float>*> MapEntry;
typedef std::map<std::wstring, std::vector<float>*> Layer;
static IEvaluateModel<float> *model;
static std::map<std::wstring, size_t> inDims;
static std::map<std::wstring, size_t> outDims;

void CNTKEvaluator::GenerateEvaluationMode(std::string modelFilePath)
{
    OutputDebugStringW(L"GenerateEvaluationMode\n");
	struct stat statBuf;
	if (stat(modelFilePath.c_str(), &statBuf) != 0)
	{
        OutputDebugStringW(L"GenerateEvaluationMode\n");
		fprintf(stderr, "Error: The model %s does not exist. Please follow instructions in README.md in <CNTK>/Examples/Image/MNIST to create the model.\n", modelFilePath.c_str());
		return;
	}

	GetEvalF(&model);

	// Load model with desired outputs
	std::string networkConfiguration;
	// Uncomment the following line to re-define the outputs (include h1.z AND the output ol.z)
	// When specifying outputNodeNames in the configuration, it will REPLACE the list of output nodes 
	// with the ones specified.
	//networkConfiguration += "outputNodeNames=\"h1.z:ol.z\"\n";
	networkConfiguration += "modelPath=\"" + modelFilePath + "\"";
	model->CreateNetwork(networkConfiguration);

	// get the model's layers dimensions
	model->GetNodeDimensions(inDims, NodeGroup::nodeInput);
	model->GetNodeDimensions(outDims, NodeGroup::nodeOutput);

	uint32 reqWidth = 224;
	uint32 reqHeight = 224;

	if (inDims.begin()->second != reqWidth * reqHeight * 3)
	{
		//fwprintf(stderr, L"Error: The input dimension for %s is %d which is not the expected size of %zd", inDims.begin()->first, inDims.begin()->second, 224 * 224 * 3);
        OutputDebugStringW(L"Error: The input dimension for inpit image is is not the expected size\n");
	}
}

std::vector<float> CNTKEvaluator::getFeatures(uint32 reqWidth, uint32 reqHeight)
{
	uint32 size = reqWidth * reqHeight * 3;

	// BGR conversion to BBB..GGG..RRR
	std::vector<float> featuresLocal;
	float v;

	// convert BGR array to BBB...GGG...RRR array
	for (int c = 0; c < 3; c++){
		for (unsigned int p = c; p < size; p = p + 3)
		{
			v = resizedImagedataArray[p];
			featuresLocal.push_back(v);
		}
	}
	return featuresLocal;
}

int CNTKEvaluator::findClass(std::vector<float> outputs)
{
	float currentMaxVal = INT_MIN;
	unsigned int maxClass = 0;
	unsigned int currClass = 0;
	std::map<int, float> valMap;
	for (auto& value : outputs)
	{
		if (value > currentMaxVal) {
			maxClass = currClass;
			currentMaxVal = value;
		}
		currClass++;
	}
	return maxClass;
}

int CNTKEvaluator::EvaluateImageClassificationModel()
{
	// Allocate the output values layer
	std::vector<float> outputs;

	auto inputLayerName = inDims.begin()->first;
	Layer inputLayer;
	inputLayer.insert(MapEntry(inputLayerName, &features));

	Layer outputLayer;
	auto outputLayerName = outDims.begin()->first;
	outputLayer.insert(MapEntry(outputLayerName, &outputs));

	// We can call the evaluate method and get back the results (single layer)...
	model->Evaluate(inputLayer, outputLayer);

	features.resize(0);

	return findClass(outputs);
}

Platform::String^ CNTKEvaluator::ClassifyImages()
{
	int ImageClass = EvaluateImageClassificationModel();
	std::string ImageClassStr = Helper::MapImageClassNumberToImageClassString(ImageClass);
	Platform::String^ ImageClassStrPath = Helper::stringToPlatformString(ImageClassStr);
	return ImageClassStrPath;
}