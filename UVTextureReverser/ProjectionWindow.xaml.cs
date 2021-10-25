using Microsoft.Win32;
using System;
using System.Windows;

namespace UVTextureReverser {
    /// <summary>
    /// Interaction logic for ProjectionWindow.xaml
    /// </summary>
    public partial class ProjectionWindow : Window {

        // Things to do here
        // 1) When an overlay image is loaded, figure out the maximum size it can be while still fitting in the projection map
        // - This will be considered "100% scale"
        // When the scale changes, the overlay image will need to be resized so that it fits these dimensions
        // Rotating the overlay is done about the center of the image

        // 2) The image should be draggable around the projection. They should be able to grab anywhere in the projection to move the overlay. They don't have to click on the center, or the actual image

        // 3) Have an option to draw the overlay slightly transparent, in case they have an image with a solid background and need to see the map

        // 4) Do the projection work on a background thread

        // 5) Add auto-projection and auto-preview updating

        private ISBitmap projectionMap;
        private ISBitmap overlay;
        private ISBitmap texture;
        
        // where to save a preview texture when the projection is updated
        private string texturePreviewPath;

        public ProjectionWindow(string projectionMapPath, string texturePreviewPath = null) {
            InitializeComponent();

            this.projectionMap = ISBitmap.fromFile(projectionMapPath);
            this.Projection.Source = projectionMap.toImageSource();
            this.SaveTexture.IsEnabled = false;
            this.texturePreviewPath = texturePreviewPath;

            if(texturePreviewPath != null) {
                Preview.IsEnabled = true;
            }
        }

        private void ButtonAddOverlay_Click(object sender, RoutedEventArgs e) {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.png;*.tga;*.jpg;*.bmp";
            if(ofd.ShowDialog() == true) {
                ISBitmap newOverlay = ISBitmap.fromFile(ofd.FileName);
                if(this.projectionMap.width != newOverlay.width || this.projectionMap.height != newOverlay.height) {
                    MessageBox.Show(String.Format("Overlay image must have same dimensions as projection map ({0} x {1}", this.projectionMap.width, this.projectionMap.height), "Laziness Error");
                } else {
                    this.overlay = newOverlay;
                }
            }

            updateOverlay();
            doProjection();
        }


        private void doProjection() {
            if (overlay != null) {
                int textureSize = Int32.Parse((String)this.ScanResolutionCombo.SelectedValue);
                int outputResolution =(1 << Int32.Parse((String)this.TextureResolutionCombo.SelectedValue));
                texture = projectionMap.project(overlay, textureSize).scale(outputResolution,outputResolution);
                if(FillHoles.IsChecked.GetValueOrDefault(false))
                {
                    texture.fillSmallHoles();
                }
                this.Texture.Source = texture.toImageSource();
                this.SaveTexture.IsEnabled = this.texture != null;
            }
        }

        private void updateOverlay() {
            if (overlay != null) {
                this.Projection.Source = projectionMap.layer(overlay).toImageSource();
                doProjection();
            } else {
                this.Projection.Source = projectionMap.toImageSource();
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Texture|*.png;*.tga;*.bmp";
            if (sfd.ShowDialog() == true) {
                this.texture.toFile(sfd.FileName);
            }
        }

        private void TextureResolutionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            doProjection();
        }

        private void ButtonLoadMapping_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Projecture Map|*.png";
            if (ofd.ShowDialog() == true) {
                this.projectionMap = ISBitmap.fromFile(ofd.FileName);
                updateOverlay();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            (new StartWindow()).Show();
            this.Close();
        }

        private void Preview_Click(object sender, RoutedEventArgs e) {
            this.texture.toFile(this.texturePreviewPath);
        }

        private void ScanResolutionCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            doProjection();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            doProjection();
        }
    }
}
