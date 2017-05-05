///-----------------------------------------------------------------
///   Class:          Helper
///   Description:    Helper String Operations and Mapping functions
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#ifndef HELPER_H
#define HELPER_H

// String and Vector Operations dependencies //
#include <collection.h>
#include <string>
#include <locale>
#include <codecvt>

class Helper{

public:
	// String operations Helper Functions
	static Platform::String^ stringToPlatformString(std::string inputString);

	static std::string PlatformStringToString(Platform::String^ inputString);

	static std::string MapImageClassNumberToImageClassStr(int ImageClass);

	static std::string MapImageClassNumberToImageClassString(int ImageClass);

};

#endif