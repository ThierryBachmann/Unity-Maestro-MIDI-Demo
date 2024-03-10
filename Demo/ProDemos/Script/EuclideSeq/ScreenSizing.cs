using UnityEngine;
using UnityEngine.UI;

namespace MPTKDemoEuclidean
{
    public class ScreenSizing : MonoBehaviour
    {
        public RectTransform ScrollView;
        public CanvasScaler scaler;
        // Use this for initialization
        void Start()
        {
            scaler = GetComponent<CanvasScaler>();
        }
        
        // Update is called once per frame
        void Update()
        {
            
            Debug.Log(" width:" + Screen.width + " height:" + Screen.height + " scaleFactor:" + scaler.scaleFactor);
            //ScrollView.sizeDelta = new Vector2(ScrollView.sizeDelta.x, 500-50);
        }
    }
}