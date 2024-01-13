using System.IO;
using Assets.Scripts.Menu.ListView;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ReferenceImagesDetails
    {
        private DetailsWidgetGroup[] views;
        private DetailsTextScript[] viewsText;
        private DetailsPropertyPairScript[] _offsetTexts;
        private DetailsPropertyPairScript[] _rotationScaleTexts;
        private DetailsPropertyPairScript[] _opacityVisibilityTexts;
        private DetailsSpacerScript[] _spacers;
        private ListViewDetailsScript _listViewDetails;

        public ReferenceImagesDetails(ListViewDetailsScript listViewDetails)
        {
            _listViewDetails = listViewDetails;
            views = new DetailsWidgetGroup[6];
            viewsText = new DetailsTextScript[6];
            _offsetTexts = new DetailsPropertyPairScript[6];
            _rotationScaleTexts = new DetailsPropertyPairScript[6];
            _opacityVisibilityTexts = new DetailsPropertyPairScript[6];
            _spacers = new DetailsSpacerScript[6];

            for (int i = 0; i < 6; i++)
            {
                views[i] = listViewDetails.Widgets.AddGroup();
                views[i].AddHeader(((ReferenceImages.Views)i).ToString());
                viewsText[i] = views[i].AddText("image name");
                _offsetTexts[i] = views[i].AddPropertyPair("OffsetX", "Rot");
                _rotationScaleTexts[i] = views[i].AddPropertyPair("OffsetY", "Scale");
                _opacityVisibilityTexts[i] = views[i].AddPropertyPair("OffsetZ", "Active");
                _spacers[i] = views[i].AddSpacer();
            }
        }

        public void UpdateDetails(CraftImagesData imagesData)
        {
            for (int i = 0; i < 6; i++) views[i].Visible = false;

            foreach (ReferenceImageData imgData in imagesData.ImageList)
            {
                int i = (int)imgData.View;
                viewsText[i].Text = imgData.ImageName;
                _offsetTexts[i].LeftValueText = imgData.OffsetX.ToString();
                _offsetTexts[i].RightValueText = imgData.Rotation.ToString();
                _rotationScaleTexts[i].LeftValueText = imgData.OffsetY.ToString();
                _rotationScaleTexts[i].RightValueText = imgData.Scale.ToString();
                _opacityVisibilityTexts[i].LeftValueText = imgData.OffsetZ.ToString();
                _opacityVisibilityTexts[i].RightValueText = imgData.Active.ToString();

                if (!File.Exists(Mod.Instance.refImagePath + imgData.ImageName)) viewsText[i].Text += ("   " + Mod.Instance.errorColor + "MISSING");

                views[i].Visible = true;
            }
        }
    }
}