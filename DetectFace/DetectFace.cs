//----------------------------------------------------------------------------
//  Copyright (C) 2004-2018 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
#if !(__IOS__ || NETFX_CORE)
using Emgu.CV.Cuda;
#endif

public static class DetectFace
{
    public static void Detect(
       Bitmap bimage, String faceFileName,
       List<Rectangle> faces, out long detectionTime)
    {
        Stopwatch watch;

        IImage image = new Image<Bgr, byte>(bimage).ToUMat();

        using (InputArray iaImage = image.GetInputArray())
        {

#if !(__IOS__ || NETFX_CORE)
            if (iaImage.Kind == InputArray.Type.CudaGpuMat && CudaInvoke.HasCuda)
            {
                using (CudaCascadeClassifier face = new CudaCascadeClassifier(faceFileName))
                {
                    face.ScaleFactor = 1.1;
                    face.MinNeighbors = 10;
                    face.MinObjectSize = Size.Empty;
                    watch = Stopwatch.StartNew();
                    using (CudaImage<Bgr, Byte> gpuImage = new CudaImage<Bgr, byte>(image))
                    using (CudaImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
                    using (GpuMat region = new GpuMat())
                    {
                        face.DetectMultiScale(gpuGray, region);
                        Rectangle[] faceRegion = face.Convert(region);
                        faces.AddRange(faceRegion);
                    }
                    watch.Stop();
                }
            }
            else
#endif
            {
                //Read the HaarCascade objects
                using (CascadeClassifier face = new CascadeClassifier(faceFileName))
                {
                    watch = Stopwatch.StartNew();

                    using (UMat ugray = new UMat())
                    {
                        CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                        //normalizes brightness and increases contrast of the image
                        CvInvoke.EqualizeHist(ugray, ugray);

                        //Detect the faces  from the gray scale image and store the locations as rectangle
                        //The first dimensional is the channel
                        //The second dimension is the index of the rectangle in the specific channel                     
                        Rectangle[] facesDetected = face.DetectMultiScale(
                           ugray,
                           1.1,
                           10,
                           new Size(50, 50));

                        faces.AddRange(facesDetected);
                    }
                    watch.Stop();
                }
            }
            detectionTime = watch.ElapsedMilliseconds;
        }
    }

    public static Bitmap Show(Bitmap bimage, List<Rectangle> faces)
    {
        IImage image = new Image<Bgr, byte>(bimage).ToUMat();

        foreach (Rectangle face in faces)
            CvInvoke.Rectangle(image, face, new Bgr(Color.Green).MCvScalar, 2);

        return image.Bitmap;
    }

    public static List<Bitmap> SeprateFace(Bitmap bmap, List<Rectangle> faces)
    {
        Image<Bgr, byte> cvSource = new Image<Bgr, byte>(bmap);

        List<Bitmap> faceimgs = new List<Bitmap>(faces.Count);

        foreach (Rectangle face in faces)
        {
            Image<Bgr, byte> cvClone = cvSource.Clone();
            cvClone.ROI = face;
            faceimgs.Add(cvClone.Bitmap);
        }

        return faceimgs;
    }

    public static Bitmap[] SeprateTo4Faces(Bitmap bmap, List<Rectangle> faces)
    {
        Image<Bgr, byte> cvSource = new Image<Bgr, byte>(bmap);

        List<Bitmap> sepratefaces = new List<Bitmap>();

        foreach (Rectangle face in faces)
        {
            Image<Bgr, byte> cvClone = cvSource.Clone();
            cvClone.ROI = face;
            sepratefaces.Add(cvClone.Bitmap);
        }

        Bitmap[] retfaces = new Bitmap[4];

        if (sepratefaces.Count >= 4)
        {
            sepratefaces.CopyTo(0, retfaces, 0, 4);
        }
        else if (sepratefaces.Count < 4)
        {
            var num = sepratefaces.Count;
            sepratefaces.CopyTo(0, retfaces, 0, num);
            for (int i = 3; i >= num; i--)
            {
                retfaces[i] = new Bitmap("default_head.jpg");
            }
        }
        return retfaces;
    }

    public static Bitmap Bgr2Gray(Bitmap src)
    {
        Bitmap bimage = (Bitmap)src.Clone();
        IImage image = new Image<Gray, byte>(bimage).ToUMat();
        return image.Bitmap;
    }

    public static Bitmap EqualizerHistMap(Bitmap src)
    {
        Bitmap bimage = (Bitmap)src.Clone();
        IImage image = new Image<Gray, byte>(bimage).ToUMat();

        IImage output = (IImage)image.Clone();

        CvInvoke.EqualizeHist(image, output);

        return output.Bitmap;
    }

    public static Bitmap PickOneBigFace(Bitmap bmap, List<Rectangle> faces)
    {
        Image<Bgr, byte> cvSource = new Image<Bgr, byte>(bmap);

        long maxsize = 0;
        Rectangle maxface = new Rectangle();

        foreach (Rectangle face in faces)
        {
            if (face.Height * face.Width >= maxsize)
            {
                maxsize = face.Height * face.Width;
                maxface = face;
            }
        }
        cvSource.ROI = maxface;

        return cvSource.Bitmap;
    }
}
