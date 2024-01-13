using System.IO;
using Assets.Scripts.Menu.ListView;
using UnityEngine;


namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ReferenceImageDetails
    {
        private DetailsTextScript _description;
        private DetailsImageScript _image;

        public ReferenceImageDetails(ListViewDetailsScript listViewDetails)
        {
            _image = listViewDetails.Widgets.AddImage();
            _image.SetSize(200);
            _description = listViewDetails.Widgets.AddText("Description");
            listViewDetails.Widgets.AddSpacer();
        }

        public void UpdateDetails(string image)
        {
            Texture2D img = new Texture2D(0, 0);
            img.LoadImage(File.ReadAllBytes(image));
            _image.SetSize(500 * img.height / img.width);
            _image.Visible = true;
            _image.ImagePath = image;
            _description.Text = image;
        }
    }
}