using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MudClient {
    internal partial class MapWindow : Form {

        internal MapWindow() {
            InitializeComponent();
            LoadData();
        }

        internal void LoadData() {
            Task.Run(() => {
                MapData.Load();

                this.Invalidate();
            });
        }

        // todo: tidy up all this scaling and offset stuff
        private void OnPaint(object sender, PaintEventArgs e) {
            MapRenderer.Render(e);
        }

        private void OnResizeEnd(object sender, EventArgs e) {
            ((Control)sender).Invalidate();
        }

        private void MapWindow_SizeChanged(object sender, EventArgs e) {
            ((Control)sender).Invalidate();
        }
    }
}
