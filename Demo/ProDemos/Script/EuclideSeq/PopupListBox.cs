using MidiPlayerTK;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupListBox : MonoBehaviour
{

    public Text TxtTitle;
    public InputField InputFilter;
    public Toggle ToggleKeepOpen;
    public string Title;
    public BtItem TemplateButton;
    public RectTransform ContentScroller;
    public EventSelect OnEventSelect;
    public UnityEvent OnEventClose;

    List<BtItem> listBt;


    public int Count
    {
        get { return listBt.Count; }
    }

    [System.Serializable]
    public class EventSelect : UnityEvent<MPTKListItem>
    {
    }

    public PopupListBox Create(string title)
    {
        PopupListBox popup = Instantiate<PopupListBox>(this);
        popup.Title = title;
        popup.transform.position = this.transform.position;
        popup.transform.SetParent(this.transform.parent);
        popup.transform.localScale = new Vector3(1, 1, 1);
        ((RectTransform)popup.transform).sizeDelta = ((RectTransform)this.transform).sizeDelta;
        BtItem.ColSelected = new Color(0xD8 / 255f, 0xE1 / 255f, 0xDB / 255f); //D8 E1 DB B8
        BtItem.ColUnselected = new Color(0x7C / 255f, 0xBD / 255f, 0xA8 / 255f);

        return popup;
    }

    // Use this for initialization
    void Start()
    {
        TemplateButton.gameObject.SetActive(false);
        TxtTitle.text = Title;
        InputFilter.onValueChanged.AddListener((string info) =>
        {
            Debug.Log($"onValueChanged '{info}'");
            foreach (BtItem bt in listBt)
                if (bt.Item.Label.ToLower().Contains(info.ToLower()))
                    bt.gameObject.SetActive(true);
                else
                    bt.gameObject.SetActive(false);

        });
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
        if (OnEventClose != null)
            OnEventClose.Invoke();
    }

    public int RandomIndex()
    {
        return listBt[Random.Range(0, Count)].Item.Index;
    }

    public int FirstIndex()
    {
        return listBt[0].Item.Index;
    }
    public string LabelSelected(int index)
    {
        foreach (BtItem bt in listBt)
            if (bt.Item.Index == index)
                return bt.Item.Label;
        return "Unknown";
    }

    public void Select(int index)
    {
        //Debug.Log($"Select bt {index}");
        foreach (BtItem bt in listBt)
            if (bt.Item.Index == index)
                bt.ImgSelect.color = BtItem.ColSelected;
            else
                bt.ImgSelect.color = BtItem.ColUnselected;
    }

    public void AddItem(MPTKListItem info)
    {
        BtItem butItem = TemplateButton.Create(info);
        butItem.ButSelect.onClick.AddListener(() =>
        {
            if (OnEventSelect != null)
                OnEventSelect.Invoke(butItem.Item);
            if (!ToggleKeepOpen.isOn)
                Close();
        });

        if (listBt == null)
            listBt = new List<BtItem>();
        listBt.Add(butItem);

        // Resize the content of the scroller to reflect the position of the scroll bar (100=height of PanelController + space)
        ContentScroller.sizeDelta = new Vector2(ContentScroller.sizeDelta.x, (listBt.Count / 5) * 40);
        ContentScroller.ForceUpdateRectTransforms();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
