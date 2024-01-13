using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Menu.ListView;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ReferenceImagesManager : ListViewModel
    {
        public string Title { get; set; } = "Reference Image Manager";
        private Mod _mod = Mod.Instance;
        private List<String> _refImages;
        private ReferenceImagesDetails _details;
        private CraftImagesDataBase _imageDB;
        public bool missingImages;

        public ReferenceImagesManager(CraftImagesDataBase imageDB)
        {
            _imageDB = imageDB;
        }

        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.Title = Title;
            listView.CanDelete = true;
            listView.PrimaryButtonText = "COPY DATA";
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;

            this.NoItemsFoundMessage = ("No reference images saved");
        }

        public override IEnumerator LoadItems()
        {
            missingImages = false;
            _details = new ReferenceImagesDetails(base.ListView.ListViewDetails);
            foreach (CraftImagesData imageData in _imageDB.CraftImagesList)
            {
                ListViewItemScript listViewItemScript = base.ListView.CreateItem(imageData.CraftName, imageData.ImageList.Count() + " images", imageData);
                foreach (ReferenceImageData image in imageData.ImageList)
                {
                    if (!File.Exists(_mod.refImagePath + image.ImageName))
                    {
                        listViewItemScript.StatusIcon = ListViewItemScript.StatusIconType.Exclamation;
                        listViewItemScript.StatusIconColor = "#b33e46";
                        listViewItemScript.StatusIconTooltip = "Missing Image";
                        missingImages = true;
                        break;
                    }
                }

            }
            yield return new WaitForEndOfFrame();
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                _details.UpdateDetails(item.ItemModel as CraftImagesData);
            }
            completeCallback?.Invoke();
        }

        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            CraftImagesData data = selectedItem.ItemModel as CraftImagesData;
            _mod.dataManager.copiedImageData = data;
            _mod.Designer.DesignerUi.ShowMessage("Copied images from '" + data.CraftName + "'");
        }

        public override void OnDeleteButtonClicked(ListViewItemScript selectedItem)
        {   
            CraftImagesData imageData = selectedItem.ItemModel as CraftImagesData;
            MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
            messageDialogScript.OkayButtonText = "DELETE";
            messageDialogScript.MessageText = string.Format("Confirm that you want to delete the images for '" + imageData.CraftName + "'");
            messageDialogScript.UseDangerButtonStyle = true;
            messageDialogScript.OkayClicked += delegate (MessageDialogScript d)
            {
                d.Close();
                _mod.DeleteImageData(imageData.CraftName);
                base.ListView.DeleteItem(selectedItem);
                Items.Remove(selectedItem);
                base.ListView.SelectedItem = null;
            };
        }
    }
}