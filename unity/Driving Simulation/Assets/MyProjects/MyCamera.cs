using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils;

public class MyCamera : MonoBehaviour
{
    public RenderTexture render_texture;
    public Material processed_material;

    int row = 0;
    int col = 0;
    Mat cameraMat;
    Texture2D texture;

    Point pt_center;
    List<Point> pt_refs;
    int num_refs = 5;
    float[] distances;
    Text[] text_distances;

    public float[] GetDistances()
    {
        return distances;
    }

    // Start is called before the first frame update
    void Start()
    {
        row = render_texture.width;
        col = render_texture.height;
        cameraMat = new Mat (col, row, CvType.CV_8UC4);
        texture = new Texture2D (cameraMat.width (), cameraMat.height (), TextureFormat.RGBA32, false);

        pt_center = new Point(63, 127);
        pt_refs = new List<Point>();
        pt_refs.Add(new Point(0, 85));
        pt_refs.Add(new Point(0, 0));
        pt_refs.Add(new Point(63, 0));
        pt_refs.Add(new Point(127, 0));
        pt_refs.Add(new Point(127, 85));

        distances = new float[num_refs];
        text_distances = new Text[num_refs];
        text_distances[0] = GameObject.Find("UI/Text").GetComponent<Text>();
        text_distances[1] = GameObject.Find("UI/Text (1)").GetComponent<Text>();
        text_distances[2] = GameObject.Find("UI/Text (2)").GetComponent<Text>();
        text_distances[3] = GameObject.Find("UI/Text (3)").GetComponent<Text>();
        text_distances[4] = GameObject.Find("UI/Text (4)").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void Capture()
    {
        Utils.textureToTexture2D (render_texture, texture);
        Utils.texture2DToMat (texture, cameraMat);

        Imgproc.cvtColor (cameraMat, cameraMat, Imgproc.COLOR_RGBA2GRAY);
        Imgproc.threshold (cameraMat, cameraMat, 100, 255, Imgproc.THRESH_BINARY);

        foreach (Point pt in pt_refs){
            int index = pt_refs.IndexOf(pt);
            distances[index] = GetDistanceToWall(pt_center, pt);
        }

        Imgproc.cvtColor (cameraMat, cameraMat, Imgproc.COLOR_GRAY2RGBA);
        foreach (Point pt in pt_refs){
            Imgproc.line(cameraMat, pt_center, pt, new Scalar(0, 255, 0, 255), 1);
        }
        Utils.fastMatToTexture2D(cameraMat, texture);
        processed_material.mainTexture = texture;
        
        for (int i = 0; i < 5; i++) text_distances[i].text = distances[i].ToString("0.0");
    }

    float GetDistanceToWall(Point pt_start, Point pt_end)
    {
        float distance = 0;
        int check_x = 0;
        int check_y = 0;
        for (int i = (int)pt_start.y; i >= (int)pt_end.y; i--){
            check_x = (int)Math.Round(pt_start.x + ((pt_end.x - pt_start.x)/(pt_start.y - pt_end.y) * (pt_start.y - i)), 1);
            check_y = i;
            double[] buff = cameraMat.get(check_y, check_x);
            if(buff[0] != 0)
                break;
            distance++;
        }
        Imgproc.circle(cameraMat, new Point(check_x, check_y), 2, new Scalar(100), 1);
        //Debug.Log((check_x, check_y, distance));
        return distance;
    }
}
