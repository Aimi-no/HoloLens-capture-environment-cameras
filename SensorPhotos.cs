using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.WSA.WebCam;
using System.Linq;
using UnityEngine.Experimental.UIElements;
using System.IO;
using System.Text;
using Windows.Media.Capture.Frames;


#if UNITY_UWP
using System;

using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class SensorPhotos : MonoBehaviour {

    string BASEURL = "http://10.35.100.210:9099";
    public AudioSource audioData;
    PhotoCapture photoCaptureObject = null;
    Resolution m_cameraResolution;
    string name;
   

    // Use this for initialization
    async void Start () {
        name = Time.time.ToString();

        //audioData = GetComponent<AudioSource>();
#if WINDOWS_UWP
        //TakePhotoWMediaCapture();
#endif

        TakePhotoOther();

        /*InitSensor(0);
        InitSensor(1);
        InitSensor(2);
        InitSensor(3);*/


        InitSensor(4);
        InitSensor(5);
        InitSensor(6);
        InitSensor(7);

    }
	
	// Update is called once per frame
	void Update () {
	}


    async void TakePhotoOther()
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[1];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[2];
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            //PhotoCaptureSource = PhotoCaptureSource.Photo,
        };
        try
        {
            await mediaCapture.InitializeAsync(settings);


            var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
            mediaframereader.FrameArrived += (sender, e) => FrameArrived(sender, e, 0);
            await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }


#if WINDOWS_UWP

    async void TakePhotoWMediaCapture()
    {
        byte[] data = await GetPhotoAsync();
        UploadImage(data, 0);
    }


    public async System.Threading.Tasks.Task<byte[]> GetPhotoAsync()
    {
        Debug.Log("1\n");
        //Get available devices info
        var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
            Windows.Devices.Enumeration.DeviceClass.VideoCapture);
        var numberOfDevices = devices.Count;
        Debug.Log("2\n");
        byte[] photoBytes = null;

        //Check if the device has camera
        if (devices.Count > 0)
        {
            Debug.Log("3\n");
            Windows.Media.Capture.MediaCapture mediaCapture = new Windows.Media.Capture.MediaCapture();
            await mediaCapture.InitializeAsync();
            Debug.Log("3.5\n");
            //Get Highest available resolution
            var tmp = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(
                Windows.Media.Capture.MediaStreamType.Photo);
           var highestResolution = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(
                Windows.Media.Capture.MediaStreamType.Photo).
                Select(item => item as Windows.Media.MediaProperties.ImageEncodingProperties).
                Where(item => item != null).
                OrderByDescending(Resolution => Resolution.Height * Resolution.Width).
                ToList().First();
            Debug.Log("4\n");
            using (var photoRandomAccessStream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
            {
                await mediaCapture.CapturePhotoToStreamAsync(highestResolution, photoRandomAccessStream);
                Debug.Log("5\n");
                //Covnert stream to byte array
                photoBytes = await ConvertFromInMemoryRandomAccessStreamToByteArrayAsync(photoRandomAccessStream);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("No camera device detected!");
        }
        Debug.Log("6\n");
        return photoBytes;
    }

    public static async System.Threading.Tasks.Task<byte[]> ConvertFromInMemoryRandomAccessStreamToByteArrayAsync(
        Windows.Storage.Streams.InMemoryRandomAccessStream inMemoryRandomAccessStream)
    {
        using (var dataReader = new Windows.Storage.Streams.DataReader(inMemoryRandomAccessStream.GetInputStreamAt(0)))
        {
            var bytes = new byte[inMemoryRandomAccessStream.Size];
            await dataReader.LoadAsync((uint)inMemoryRandomAccessStream.Size);
            dataReader.ReadBytes(bytes);

            return bytes;
        }
    }
#endif



    void TakePhotoWPhotoCapture()
    {
        //audioData.Play(0);
        Debug.Log("Taking image\n");

        

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            Debug.Log("1\n");
            photoCaptureObject = captureObject;


            m_cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();
            Debug.Log("2\n");
            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = m_cameraResolution.width;
            cameraParameters.cameraResolutionHeight = m_cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.JPEG;
            Debug.Log("3\n");
            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                // Take a picture
                Debug.Log("4\n");
                
                //tady to konci
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }


    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        //Debug.Log("Stopped photo mode");
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            //Debug.Log("\n Image taken \n");
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("Unable to start photo mode!");
        }
    }

    //Get the image, pose of camera 
    private async void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Debug.Log("5\n");
        if (result.success)
        {
            //Debug.Log("\n Saving picture \n");
            List<byte> imageBufferList = new List<byte>();

            // Copy the raw IMFMediaBuffer data into our empty byte list.
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);

            UploadImage(imageBufferList.ToArray(), 0);

            /*if ( sceneId > 0)
            {
               UploadImageToScene(imageBufferList.ToArray(), sceneId);
            }*/

        }
        // Clean up
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }


    void UploadImage(byte[] bytes, int sensor)
    {

        Debug.Log("Image taken");
        string url = BASEURL + "/api/cv/save_sensor_data/";
        //string url = "https://enan5e9uyvbpe.x.pipedream.net/";
        WWWForm form = new WWWForm();
        form.AddField("sensor", sensor.ToString());
        form.AddField("name", name);
        form.AddBinaryData("jpgdata", bytes);
        
        //form.AddBinaryData("jpgdata", ImageConversion.EncodeToJPG(tex, 75).ToArray());
        // Construct Form data



        UnityWebRequest request = UnityWebRequest.Post(url, form);


        // Fire request
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        op.completed += ImageUploaded;
    }

    //handler for http request
    void ImageUploaded(AsyncOperation op)
    {
        UnityWebRequestAsyncOperation uop = (UnityWebRequestAsyncOperation)op;
    }

#if UNITY_UWP

    private async void InitSensor(int sensor)
    {
        var mediaFrameSourceGroupList = await MediaFrameSourceGroup.FindAllAsync();
        var mediaFrameSourceGroup = mediaFrameSourceGroupList[0];
        var mediaFrameSourceInfo = mediaFrameSourceGroup.SourceInfos[sensor];
        var mediaCapture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = mediaFrameSourceGroup,
            SharingMode = MediaCaptureSharingMode.SharedReadOnly,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            //PhotoCaptureSource = PhotoCaptureSource.Photo,
    };
        try
        {
            await mediaCapture.InitializeAsync(settings);


             var mediaFrameSource = mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
             var mediaframereader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource, mediaFrameSource.CurrentFormat.Subtype);
             mediaframereader.FrameArrived += (sender, e) => FrameArrived(sender, e, sensor);
             await mediaframereader.StartAsync();
        }
        catch (Exception e)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { Debug.Log(e); }, true);
        }
    }

    private void FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args, int sensor)
    {
        byte[] bytes = null;
        Texture2D tex = null;
        var mediaframereference = sender.TryAcquireLatestFrame();
        if (mediaframereference != null)
        {
            var videomediaframe = mediaframereference?.VideoMediaFrame;
            var softwarebitmap = videomediaframe?.SoftwareBitmap;
            if (softwarebitmap != null)
            {
                softwarebitmap = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                int w = softwarebitmap.PixelWidth;
                int h = softwarebitmap.PixelHeight;
                if (bytes == null)
                {
                    bytes = new byte[w * h * 4];
                }
                softwarebitmap.CopyToBuffer(bytes.AsBuffer());
                softwarebitmap.Dispose();
                UnityEngine.WSA.Application.InvokeOnAppThread(() => {
                    if (tex == null)
                    {
                        tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        //GetComponent<Renderer>().material.mainTexture = tex;
                    }

                    tex.LoadRawTextureData(bytes);
                    //tex.Apply();
                    //Debug.Log("1");
                    
                    UploadImage(ImageConversion.EncodeToJPG(tex,100), sensor);
                }, true);
            }
            mediaframereference.Dispose();
            sender.StopAsync();
        }


        //tady to ukoncit
    }
#endif


}
