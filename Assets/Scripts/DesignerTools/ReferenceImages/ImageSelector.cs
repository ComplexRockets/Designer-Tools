using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Menu.ListView;
using Assets.Scripts.State;
using ModApi.Craft;
using ModApi.Math;
using ModApi.State;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ImageSelector : ListViewModel
    {
        public string PrimaryButtonText { get; set; } = "SELECT";
        private List<String> _images;
        public Action<String> ImageSelected { get; internal set; }
        private ReferenceImageDetails _details;
        private string _view;

        public ImageSelector(List<String> images, string view)
        {
            _images = images;
            _view = view;
        }

        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.Title = "Select " + _view + " image";
            listView.CanDelete = false;
            listView.PrimaryButtonText = PrimaryButtonText;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
            this.NoItemsFoundMessage = ("No image found, drop your images in " + Mod.Instance.refImagePath);
        }

        public override IEnumerator LoadItems()
        {
            _details = new ReferenceImageDetails(base.ListView.ListViewDetails);
            foreach (string image in _images)
            {
                string[] data = image.Split('/').Last().Split('.');
                string name = data[0];
                string type = data[1];
                ListViewItemScript listViewItemScript = base.ListView.CreateItem(name, type, image);
            }
            yield return new WaitForEndOfFrame();
        }

        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            ImageSelected?.Invoke(selectedItem?.ItemModel as string);
            base.ListView.Close();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                _details.UpdateDetails(item.ItemModel as string);
            }
            completeCallback?.Invoke();
        }
    }
}