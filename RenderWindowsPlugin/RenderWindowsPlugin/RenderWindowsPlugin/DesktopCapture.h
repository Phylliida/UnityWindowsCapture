// Don't worry I don't normally stick all my code in the header file
// This was just the easiest way to compile a simple plugin since there isn't that much code

#include <d3d11_2.h>


#include <iostream>


#include <string> 
#include <vector>






#ifdef TESTFUNCDLL_EXPORT
#define TESTFUNCDLL_API __declspec(dllexport) 
#else
#define TESTFUNCDLL_API __declspec(dllimport) 
#endif

typedef void(*FuncPtr)(const char *);

FuncPtr Debug;

extern "C" {;
	TESTFUNCDLL_API int InitDeskDupl(void* dummyTexture, int outputNum);
	TESTFUNCDLL_API void CleanupDeskDupl(int outputNum);
	TESTFUNCDLL_API void GetDesktopFrame(void* dummyTexture, int outputNum, int &width, int &height, byte* curData, int lenCurData, int timeoutInMillis);

	TESTFUNCDLL_API void SetDebugFunction(FuncPtr fp);
}




void SetDebugFunction(FuncPtr fp)
{
	Debug = fp;
}



class DesktopCapturer
{
public:
	IDXGIOutputDuplication* deskDupl;
	DXGI_OUTPUT_DESC outputDesc;
	ID3D11Texture2D* acquiredDesktopImage;
	UINT metaDataSize;
	byte* metaDataBuffer;
	ID3D11Texture2D* desktopImage = nullptr;
	DXGI_OUTDUPL_FRAME_INFO frameInfo;

	ID3D11Texture2D* copyTexture = nullptr;

	int hwnd;

	DesktopCapturer(void* dummyTexture, int outputNum);
	void GetDesktopFrame(void* dummyTexture, int &width, int &height, byte* curData, int lenCurData, int timeoutInMillis);
	void CleanupDeskDupl();
};


static std::vector<int> displayNums;
static std::vector<DesktopCapturer*> desktopCapturers;

void GetDesktopFrame(void* dummyTexture, int outputNum, int &width, int &height, byte* curData, int lenCurData, int timeoutInMillis)
{
	// It is okay to loop every frame because this vector is very small (6 if they have, say, 6 windows)
	for (int i = 0; i < displayNums.size(); i++)
	{
		if (displayNums[i] == outputNum)
		{
			desktopCapturers[i]->GetDesktopFrame(dummyTexture, width, height, curData, lenCurData, timeoutInMillis);
		}
	}
}

int InitDeskDupl(void* dummyTexture, int outputNum)
{
	DesktopCapturer* desktopCapturer = new DesktopCapturer(dummyTexture, outputNum);
	if (desktopCapturer->hwnd == -1)
	{
		delete desktopCapturer;
		return -1;
	}
	else
	{
		displayNums.push_back(outputNum);
		desktopCapturers.push_back(desktopCapturer);
	}
}


void CleanupDeskDupl(int outputNum)
{
	for (int i = 0; i < displayNums.size(); i++)
	{
		if (displayNums[i] == outputNum)
		{
			desktopCapturers[i]->CleanupDeskDupl();
			delete desktopCapturers[i];
			displayNums.erase(displayNums.begin() + i);
			desktopCapturers.erase(desktopCapturers.begin() + i);
			// Don't break in case we somehow added one twice
		}
	}
}


// OutputNum = 0 for example
DesktopCapturer::DesktopCapturer(void* dummyTexture, int outputNum)
{
	ID3D11Texture2D* originalTexture = (ID3D11Texture2D*)dummyTexture;
	ID3D11Device* d3dx11Device = nullptr;
	originalTexture->GetDevice(&d3dx11Device);

	

	IDXGIDevice* dxgiDevice = nullptr;
	d3dx11Device->QueryInterface(__uuidof(IDXGIDevice), reinterpret_cast<void**>(&dxgiDevice));














	IDXGIAdapter* dxgiAdapter = nullptr;
	dxgiDevice->GetParent(__uuidof(IDXGIAdapter), reinterpret_cast<void**>(&dxgiAdapter));

	dxgiDevice->Release();


	//IDXGIFactory2* factory2 = nullptr;
	//dxgiAdapter->GetParent(__uuidof(IDXGIFactory2), reinterpret_cast<void**>(&factory2));


	IDXGIOutput* dxgiOutput = nullptr;
	dxgiAdapter->EnumOutputs(outputNum, &dxgiOutput);
	
	dxgiAdapter->Release();

	ZeroMemory(&outputDesc, sizeof(outputDesc));
	dxgiOutput->GetDesc(&outputDesc);

	HMONITOR monitorHandle = outputDesc.Monitor;

	IDXGIOutput1* dxgiOutput1 = nullptr;
	dxgiOutput->QueryInterface(__uuidof(dxgiOutput1), reinterpret_cast<void**>(&dxgiOutput1));
	
	dxgiOutput->Release();

	dxgiOutput1->DuplicateOutput(d3dx11Device, &deskDupl);
	dxgiOutput1->Release();

	acquiredDesktopImage = nullptr;
	metaDataSize = 0;
	metaDataBuffer = nullptr;




























	//DWORD occlusionCookie = 0;


	
	// Register for occlusion status windows message
	//factory2->RegisterOcclusionStatusWindow(Window, OCCLUSION_STATUS_MSG, &occlusionCookie);




	
	// Create swapchain for window

	/*
	IDXGISwapChain1* swapChain = nullptr;
	DXGI_SWAP_CHAIN_DESC1 SwapChainDesc;
	RtlZeroMemory(&SwapChainDesc, sizeof(SwapChainDesc));

	SwapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
	SwapChainDesc.BufferCount = 2;
	SwapChainDesc.Width = screenWidth;
	SwapChainDesc.Height = screenHeight;
	SwapChainDesc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	SwapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
	SwapChainDesc.SampleDesc.Count = 1;
	SwapChainDesc.SampleDesc.Quality = 0;
	factory2->CreateSwapChainForHwnd(d3dx11Device, Window, &SwapChainDesc, nullptr, nullptr, &swapChain);

	// Disable the ALT-ENTER shortcut for entering full-screen mode
	factory2->MakeWindowAssociation(Window, DXGI_MWA_NO_ALT_ENTER);

	*/

	// Create shared texture
	//DUPL_RETURN Return = CreateSharedSurf(SingleOutput, OutCount, DeskBounds);


	if ((int)deskDupl == 0)
	{
		hwnd = -1;
	}
	else
	{
		hwnd = (int)monitorHandle;
	}

}



void DesktopCapturer::CleanupDeskDupl()
{
	if (deskDupl)
	{
		deskDupl->Release();
	}

	//if (copyTexture)
	//{
	//	copyTextureShaderResourceView->Release();
	//	copyTexture->Release();
	//
	//}
}







void DesktopCapturer::GetDesktopFrame(void* dummyTexture, int &width, int &height, byte* curData, int lenCurData, int timeoutInMillis)
{
	IDXGIResource* DesktopResource = nullptr;
	DXGI_OUTDUPL_FRAME_INFO FrameInfo;


	std::cout << "first\n";

	HRESULT hr = deskDupl->AcquireNextFrame(timeoutInMillis, &FrameInfo, &DesktopResource);



	if (hr == DXGI_ERROR_WAIT_TIMEOUT)
	{
		return;
	}


	if (FAILED(hr))
	{
		return;
	}

	if (acquiredDesktopImage)
	{
		acquiredDesktopImage->Release();
		acquiredDesktopImage = nullptr;
	}

	hr = DesktopResource->QueryInterface(__uuidof(ID3D11Texture2D), reinterpret_cast<void **>(&acquiredDesktopImage));
	DesktopResource->Release();
	DesktopResource = nullptr;


	if (FAILED(hr))
	{
		return;
		//return ProcessFailure(nullptr, L"Failed to QI for ID3D11Texture2D from acquired IDXGIResource in DUPLICATIONMANAGER", L"Error", hr);
	}

	// Get metadata
	/*
	if (FrameInfo.TotalMetadataBufferSize)
	{
		// Old buffer too small
		if (FrameInfo.TotalMetadataBufferSize > metaDataSize)
		{
			if (metaDataBuffer)
			{
				delete[] metaDataBuffer;
				metaDataBuffer = nullptr;
			}
			metaDataBuffer = new (std::nothrow) byte[FrameInfo.TotalMetadataBufferSize];
			if (!metaDataBuffer)
			{
				metaDataSize = 0;
				//Data->MoveCount = 0;
				//Data->DirtyCount = 0;
				//return ProcessFailure(nullptr, L"Failed to allocate memory for metadata in DUPLICATIONMANAGER", L"Error", E_OUTOFMEMORY);
				return;
			}
			metaDataSize = FrameInfo.TotalMetadataBufferSize;
		}

		UINT BufSize = FrameInfo.TotalMetadataBufferSize;

		// Get move rectangles
		hr = deskDupl->GetFrameMoveRects(BufSize, reinterpret_cast<DXGI_OUTDUPL_MOVE_RECT*>(metaDataBuffer), &BufSize);
		if (FAILED(hr))
		{
			//Data->MoveCount = 0;
			//Data->DirtyCount = 0;
			//return ProcessFailure(nullptr, L"Failed to get frame move rects in DUPLICATIONMANAGER", L"Error", hr, FrameInfoExpectedErrors);

			return;
		}
		//Data->MoveCount = BufSize / sizeof(DXGI_OUTDUPL_MOVE_RECT);
		moveCount = BufSize / sizeof(DXGI_OUTDUPL_MOVE_RECT);

		BYTE* DirtyRects = metaDataBuffer + BufSize;
		BufSize = FrameInfo.TotalMetadataBufferSize - BufSize;

		// Get dirty rectangles
		hr = deskDupl->GetFrameDirtyRects(BufSize, reinterpret_cast<RECT*>(DirtyRects), &BufSize);
		if (FAILED(hr))
		{
			//Data->MoveCount = 0;
			//Data->DirtyCount = 0;
			//return ProcessFailure(nullptr, L"Failed to get frame dirty rects in DUPLICATIONMANAGER", L"Error", hr, FrameInfoExpectedErrors);
			return;
		}
		dirtyCount = BufSize / sizeof(RECT);
		metaData = metaDataBuffer;
	}

	*/

	ID3D11Texture2D* desktopImage = acquiredDesktopImage;
	frameInfo = FrameInfo;






	D3D11_TEXTURE2D_DESC SrcDesc;
	desktopImage->GetDesc(&SrcDesc);

	width = SrcDesc.Width;
	height = SrcDesc.Height;


	if (SrcDesc.Width == 0 || SrcDesc.Height == 0)
	{
		deskDupl->ReleaseFrame();

		if (acquiredDesktopImage)
		{
			acquiredDesktopImage->Release();
			acquiredDesktopImage = nullptr;
		}

		return;
	}
	

	//ID3D11Texture2D* originalTexture = (ID3D11Texture2D*)dummyTexture;
	ID3D11Device* d3dx11Device = nullptr;
	desktopImage->GetDevice(&d3dx11Device);

	ID3D11DeviceContext* d3context;
	d3dx11Device->GetImmediateContext(&d3context);


	D3D11_TEXTURE2D_DESC DestDesc;

	// Create texture we copy desktop contents into if we haven't already created it
	if (copyTexture != nullptr)
	{
		copyTexture->GetDesc(&DestDesc);

	}

	if (copyTexture == nullptr || DestDesc.Width != SrcDesc.Width || DestDesc.Height != SrcDesc.Height)
	{










		D3D11_TEXTURE2D_DESC textureDesc;

		// Initialize the render target texture description.
		ZeroMemory(&textureDesc, sizeof(textureDesc));

		// Setup the render target texture description.
		textureDesc.Width = SrcDesc.Width;
		textureDesc.Height = SrcDesc.Height;
		textureDesc.MipLevels = 1;
		textureDesc.ArraySize = 1;
		textureDesc.Format = SrcDesc.Format;
		textureDesc.Usage = D3D11_USAGE_STAGING;
		textureDesc.BindFlags = 0;
		textureDesc.CPUAccessFlags = D3D11_CPU_ACCESS_READ;
		textureDesc.MiscFlags = 0;
		textureDesc.SampleDesc.Count = 1;
		textureDesc.SampleDesc.Quality = 0;


		// Create the render target texture.


		//D3D11_SUBRESOURCE_DATA sSubData;
		//sSubData.pSysMem = ubImageStorage;
		//sSubData.SysMemPitch = (UINT)(1024 * 4);
		//sSubData.SysMemSlicePitch = (UINT)(1024 * 1024 * 4);

		

		d3dx11Device->CreateTexture2D(&textureDesc, NULL, &copyTexture);


		ZeroMemory(&DestDesc, sizeof(DestDesc));
		copyTexture->GetDesc(&DestDesc);
		
		/*
		// From http://stackoverflow.com/questions/29316524/directx11-cant-set-texture2d-as-shaderresourceview
		D3D11_SHADER_RESOURCE_VIEW_DESC srDesc;
		ZeroMemory(&srDesc, sizeof(srDesc));

		srDesc.Format = textureDesc.Format;
		srDesc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
		srDesc.Texture2D.MostDetailedMip = 0;
		srDesc.Texture2D.MipLevels = 1;

		ID3D11ShaderResourceView* pSRV;

		d3dx11Device->CreateShaderResourceView(copyTexture, &srDesc, &pSRV);
		//d3context->Release();
		*/
	}




	// Get frame data:

	

	





	if (lenCurData != width * height * 4)
	{
		deskDupl->ReleaseFrame();

		if (acquiredDesktopImage)
		{

			acquiredDesktopImage->Release();
			acquiredDesktopImage = nullptr;
		}


		d3context->Release();

		return;
	}



	//d3context->CopySubresourceRegion(copyTexture, 0, 0, 0, 0, acquiredDesktopImage, 0,  0, NULL, acquiredDesktopImage, 0);
	d3context->CopyResource(copyTexture, acquiredDesktopImage);

	//d3context->CopyResource((ID3D11Texture2D*)dummyTexture, copyTexture);


	D3D11_MAPPED_SUBRESOURCE resource;
	ZeroMemory(&resource, sizeof(resource));
	int SubResource = 0;


	HRESULT games = d3context->Map(copyTexture, SubResource, D3D11_MAP_READ, 0x0, &resource);



	byte* sptr = (byte*)curData;
	byte* dptr = (byte*)resource.pData;

	if (games == S_OK)
	{
		if (lenCurData > 0)
		{

			for (int y = 0; y < SrcDesc.Height; ++y)
			{
				memcpy(sptr, dptr, SrcDesc.Width * 4);
				sptr += SrcDesc.Width * 4;
				dptr += resource.RowPitch;
			}

		}
	}

	d3context->Unmap(desktopImage, SubResource);
	
	/*
	HRESULT games = d3context->Map(desktopImage, SubResource, D3D11_MAP_READ, 0x0, &resource);

	int ResourceDataSize = resource.RowPitch * SrcDesc.Height;

	byte* sptr = (byte*)curData;
	byte* dptr = (byte*)resource.pData;






	float result = 10.0f;



	if (lenCurData != width * height * 4)
	{
		deskDupl->ReleaseFrame();

		if (acquiredDesktopImage)
		{
			acquiredDesktopImage->Release();
			acquiredDesktopImage = nullptr;
		}

		return NULL;
	}
	if (games == S_OK)
	{

		if (lenCurData > 0)
		{
			for (int y = 0; y < SrcDesc.Height; ++y)
			{
				memcpy(sptr, dptr, SrcDesc.Width * 2);
				sptr += SrcDesc.Width * 2;
				dptr += resource.RowPitch;
			}

			for (int y = 0; y < SrcDesc.Height; ++y)
			{
				for (int x = 0; x < SrcDesc.Width; x++)
				{
					int pos = y * SrcDesc.Width * 2 + x * 2;
					curData[pos] = 125;
					curData[pos+1] = 125;



				}
				//memcpy(sptr, dptr, SrcDesc.Width * 4);
				//sptr += SrcDesc.Width * 4;
				//dptr += resource.RowPitch;
			}

			// From http://stackoverflow.com/questions/22444522/directx11-map-unmap-runtime-error
			//for (UINT row = 0; row < SrcDesc.Height; row++)
			//{
			//Row number * height
			//UINT rowStart = row * resource.RowPitch;
			//memcpy((void*)((long)resource.pData + rowStart), (void*)((long)pixelData + rowStart), SrcDesc.Width*4);
			//}
			/*
			for (UINT row = 0; row < SrcDesc.Width; row++)
			{
			UINT rowStart = resource.RowPitch / 4 * row;

			for (UINT col = 0; col < SrcDesc.Height; col++)
			{
			memcpy(resource.pData, pixelData, SrcDesc.Height);
			//if (pos.y + col < 0.0f) pTexels[depStart + rowStart + col] = -1.0f;
			//else pTexels[depStart + rowStart + col] = 1.0f;
			}
			}
			* /

			//for 

			//memcpy(resource.pData, pixelData, lenData);
		}
		//BYTE* mappedData = reinterpret_cast<BYTE*>(mappedResource.pData);
		//mappedData[0] = 1;


		//result = 0.0f;
		
	}
	else
	{
		//result = 1.0f;
	}



	d3context->Unmap(desktopImage, SubResource);



	*/




	deskDupl->ReleaseFrame();


	if (acquiredDesktopImage)
	{
		acquiredDesktopImage->Release();
		acquiredDesktopImage = nullptr;
	}




	//copyTexture->Release();

	//copyTexture = nullptr;


	d3context->Release();

}


