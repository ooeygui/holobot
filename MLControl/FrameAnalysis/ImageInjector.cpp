///-----------------------------------------------------------------
///   Class:          ImageInjector
///   Description:    Functions to Inject BitMap Image into the output stream
///   Author:         Jhalak Patel
///   Revision History:
///-----------------------------------------------------------------

#include "ImageInjector.h"

// dependency files
#include "ColorConversion.h"

static BYTE dataArray[outputImageWidth * outputImageHeight * imageChannels];

using namespace Windows::Graphics::Imaging;
using namespace Windows::UI::Xaml::Media::Imaging;

void ImageInjector::DrawBitmap(UINT32 m_imageWidthInPixels, UINT32 m_imageHeightInPixels)
{
	UYVY_MACRO_PIXEL** lineStart = NULL;
	DWORD imageWidthInMacroPixels = 0;

	for (DWORD y = 0; y < outputImageHeight; y++)
	{
		for (DWORD x = 0; x < outputImageWidth; x++)
		{
			rgbBmpFrame[y][x].R = dataArray[y * outputImageWidth * 3 + 3 * x + 0];
			rgbBmpFrame[y][x].G = dataArray[y * outputImageWidth * 3 + 3 * x + 1];
			rgbBmpFrame[y][x].B = dataArray[y * outputImageWidth * 3 + 3 * x + 2];
		}
	}

	ColorConversion::ConvertRGBToYUV420();

	do
	{
		// each macro pixel represents two actual pixels on the screen, with two
		// luma samples (Y1 and Y2), and one chroma sample (U + V).  Therefore,
		// each line of the image array will contain widthInPixels/2 macropixels
		imageWidthInMacroPixels = m_imageWidthInPixels / 2;

		for (DWORD y = 0; y < outputImageHeight && y < m_imageHeightInPixels; y++)
		{
			for (DWORD x = 0; x < (outputImageWidth - 1); x += 2) {
				// extract two YUV pixels of the image
				YUVTRIPLE yuvImagePixel1 = pYuv[y][x];
				YUVTRIPLE yuvImagePixel2 = pYuv[y][x + 1];

				YUY2OutputFrame[y][x].Y1 = yuvImagePixel1.Y;
				YUY2OutputFrame[y][x].Y2 = yuvImagePixel2.Y;
				YUY2OutputFrame[y][x].U = yuvImagePixel1.U;
				YUY2OutputFrame[y][x].V = yuvImagePixel1.V;
			}
		}
	} while (false);
}


task<bool> IfStorageItemExist(StorageFolder^ folder, String^ itemName)
{
	return create_task(folder->TryGetItemAsync(itemName)).then([](task<IStorageItem^> taskResult)
	{
		try
		{
			auto result = taskResult.get();
			return (result != nullptr);
		}
		catch (COMException ^ex)
		{
			return false;
		}
	});
}

void ImageInjector::WriteTextFile(String^ ImageString)
{
	StorageFolder^ folder = KnownFolders::PicturesLibrary;
	String^ textFileName = "ClassStr.txt";

	IfStorageItemExist(folder, textFileName).then([folder, textFileName, ImageString](bool exists)
	{
		if (exists)
		{
			Sleep(300);
			// file/folder exists 
			// do not do anything
		}
		else {
			create_task(folder->CreateFileAsync(textFileName, CreationCollisionOption::FailIfExists)).then([folder, textFileName, ImageString](StorageFile^ newFile) {
				create_task(folder->GetFileAsync(textFileName)).then([ImageString](StorageFile^ saveFile) {
					FileIO::WriteTextAsync(saveFile, ImageString);
				}).wait();
			}).wait();
		}
	}).wait();
}

void ImageInjector::WriteBmpFile()
{
	StorageFolder^ folder = KnownFolders::PicturesLibrary;

	String^ frameFileName = "photo.jpg";

	ColorConversion::convertRGBToRGBA(&newOriginalImagedataArray[0], &originalImagedataArray[0], inputImageHeight * inputImageWidth);

	IfStorageItemExist(folder, frameFileName).then([folder, frameFileName](bool exists)
	{
		if (exists)
		{
			// processing going on
			Sleep(100);
			// file/folder exists 
			// do not do anything
		}
		else
		{
			// create BMP File
			create_task(folder->CreateFileAsync(frameFileName, CreationCollisionOption::FailIfExists)).then([folder, frameFileName](StorageFile^ newFile) {

				// Opem BMP File for Writing
				create_task(folder->GetFileAsync(frameFileName)).then([](StorageFile^ saveFile) {

					create_task(saveFile->OpenAsync(FileAccessMode::ReadWrite)).then([](IRandomAccessStream^ streamOut)
					{
						// create bitmap encoder stream object
						create_task(BitmapEncoder::CreateAsync(BitmapEncoder::JpegEncoderId, streamOut)).then([streamOut](BitmapEncoder^ encoder)
						{
							// convert data array to platform array
							Platform::Array<BYTE>^ pixels = ref new Platform::Array<BYTE>(&newOriginalImagedataArray[0], inputImageHeight * inputImageWidth * 4);

							// set the pixel data
							encoder->SetPixelData(BitmapPixelFormat::Bgra8, BitmapAlphaMode::Ignore, inputImageWidth, inputImageHeight, 96.0, 86.0, pixels);
							create_task(encoder->FlushAsync());

						}).wait();


					}).wait();
					
				}).wait();

			}).wait();
		}
	});


}


//void ImageInjector::WriteBmpFile()
//{
//	StorageFolder^ folder = KnownFolders::PicturesLibrary;
//
//	String^ bmpfileName = "BMPFILE.bmp";
//
//	ColorConversion::convertRGBToRGBA(&newOriginalImagedataArray[0], &originalImagedataArray[0], inputImageHeight * inputImageWidth);
//
//	// Read the Data array into Buffer
//	DataWriter ^writer = ref new DataWriter();
//	writer->WriteBytes(Platform::ArrayReference<BYTE>(newOriginalImagedataArray, sizeof(newOriginalImagedataArray)));
//	IBuffer ^buffer = writer->DetachBuffer();
//
//	// create BMP File
//	create_task(folder->CreateFileAsync(bmpfileName, CreationCollisionOption::ReplaceExisting)).then([buffer](StorageFile^ newFile) {}).wait();
//
//	// Opem BMP File for Writing
//	create_task(folder->GetFileAsync(bmpfileName)).then([buffer](StorageFile^ saveFile) {
//
//		create_task(saveFile->OpenAsync(FileAccessMode::ReadWrite)).then([buffer](IRandomAccessStream^ streamOut)
//		{
//			// create bitmap encoder stream object
//			create_task(BitmapEncoder::CreateAsync(BitmapEncoder::JpegEncoderId, streamOut)).then([buffer](BitmapEncoder^ encoder)
//			{
//				DataReader^ dataReader = DataReader::FromBuffer(buffer);
//				Platform::Array<BYTE, 1>^ pixels = ref new Platform::Array<BYTE, 1>(buffer->Length);
//				dataReader->ReadBytes(pixels);
//
//				// set the pixel data
//				encoder->SetPixelData(BitmapPixelFormat::Bgra8, BitmapAlphaMode::Ignore, inputImageWidth, inputImageHeight, 96.0, 86.0, pixels);
//				create_task(encoder->FlushAsync());
//			}).wait();
//
//		}).wait();
//
//	}).wait();
//}

//void ImageInjector::WriteBmpFile()
//{
//	StorageFolder^ folder = KnownFolders::PicturesLibrary;
//
//	String^ bmpfileName = "TEST.bmp";
//
//	auto createFileTask = create_task(folder->CreateFileAsync(bmpfileName)).then([](StorageFile^ newFile) {
//		// open filestream from the storage file
//
//		create_task(newFile->OpenAsync(FileAccessMode::ReadWrite)).then([](IRandomAccessStream^ fileStream) {
//
//			DataWriter^ writer = ref new DataWriter(fileStream->GetOutputStreamAt(0));
//
//			Platform::Array<BYTE>^ dataArray = ref new Platform::Array<BYTE>(&originalImagedataArray[0],1555200);
//
//			writer->WriteBytes(dataArray);
//			
//			writer->FlushAsync();
//
//		}).wait();
//	}).wait();
//}

void ImageInjector::ReadBmpFile(String^ bmpfilepath)
{
	StorageFolder^ folder = KnownFolders::PicturesLibrary;

	auto createFileTask = create_task(folder->GetFileAsync(bmpfilepath)).then([](StorageFile^ newFile) {

		// open filestream from the storage file
		create_task(newFile->OpenAsync(FileAccessMode::Read)).then([](IRandomAccessStream^ fileStream) {

			// set buffer size from fileStream
			unsigned int bufferSize = (unsigned int) fileStream->Size;

			// declare buffer reader object and initialize it with appropriate size
			IBuffer^ buffer = ref new Buffer(bufferSize);

			// read filestream into a buffer reader object
			create_task(fileStream->ReadAsync(buffer, bufferSize, InputStreamOptions::None)).then([buffer, bufferSize](task<IBuffer^> byteReaderObjTask) {

				IBuffer^ byteReaderObj = byteReaderObjTask.get();

				// retrieve data reader object from buffer reader object
				//auto reader = byteReaderObj->FromBuffer(buffer);
				auto reader = Streams::DataReader::FromBuffer(byteReaderObj);

				// create data array to store the output
				auto data = ref new Platform::Array<BYTE>(bufferSize);

				// read bytes from reader object to data array
				reader->ReadBytes(data);

				// convert managed array to C style Array
				for (unsigned int i = 0; i < buffer->Length; i++) {
					if (i>53) {
						dataArray[i - 54] = data[i];
					}
				}
			}).wait();
		}).wait();
	}).wait();
}

void ImageInjector::OverlayOutputOnVideoFrame(String^ ImageClassStrPath, uint32 m_imageWidthInPixels, uint32 m_imageHeightInPixels) {

	// read bmp file and populate the image data array
	ReadBmpFile(ImageClassStrPath);

	// Create output Bitmap Image
	DrawBitmap(m_imageWidthInPixels, m_imageHeightInPixels);
}