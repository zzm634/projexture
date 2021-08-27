using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UVTextureReverser
{
    /// <summary>
    /// Interaction logic for ProjectionWindow.xaml
    /// </summary>
    public partial class ProjectionWindow : Window
    {

        private ZBitmap projectionMap;
        private ZBitmap overlay;
        private ZBitmap texture;
        public ProjectionWindow(ZBitmap projectionMap)
        {
            InitializeComponent();

            this.projectionMap = projectionMap;
            this.Projection.Source = projectionMap.toImageSource();
            this.SaveTexture.IsEnabled = false;
        }

        private void ButtonAddOverlay_Click(object sender, RoutedEventArgs e)
        {
            overlay = ZBitmap.randomSquiggles(projectionMap.width, projectionMap.height);
            updateOverlay();
        }

        private void ButtonProject_Click(object sender, RoutedEventArgs e)
        {
            int textureSize =  Int32.Parse((String)this.TextureResolutionCombo.SelectedValue);
            texture = projectionMap.project(overlay, textureSize);
            this.Texture.Source = texture.toImageSource();
            this.SaveTexture.IsEnabled = this.texture != null;
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Projecture Map|*.png";
            if(ofd.ShowDialog() == true)
            {
                this.projectionMap = ZBitmap.fromFile(ofd.FileName);
                updateOverlay();
            }
        }

        private void updateOverlay()
        {
            if (overlay != null)
            {
                this.Projection.Source = projectionMap.layer(overlay).toImageSource();
            } else
            {
                this.Projection.Source = projectionMap.toImageSource();
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Texture|*.png;*.tga;*.bmp";
            if (sfd.ShowDialog() == true) {             
                this.texture.toFile(sfd.FileName);
            }
        }
    }
}
