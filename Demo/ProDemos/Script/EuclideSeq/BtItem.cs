using MidiPlayerTK;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BtItem : MonoBehaviour
{
    public MPTKListItem Item;
    public Text TxtTitle;
    public Button ButSelect;
    public Image ImgSelect;
    public static Color ColSelected;
    public static Color ColUnselected;

    public BtItem Create(MPTKListItem item)
    {
        //Debug.Log("Create " + item.Label);
        BtItem butItem = Instantiate<BtItem>(this);
        butItem.transform.position = this.transform.position;
        butItem.transform.SetParent(this.transform.parent);
        // changing parent can affect scale, reset to 1
        butItem.transform.localScale = new Vector3(1, 1, 1);
        ((RectTransform)butItem.transform).sizeDelta = ((RectTransform)this.transform).sizeDelta;
        butItem.gameObject.SetActive(true);
        butItem.SetItem(item);
        return butItem;
    }

    public void SetItem(MPTKListItem item)
    {
        Item = item;
        TxtTitle.text = item.Label;
    }
}
