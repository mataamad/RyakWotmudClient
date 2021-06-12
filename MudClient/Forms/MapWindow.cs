using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MudClient {
    public partial class MapWindow : Form {

        public MapWindow() {
            InitializeComponent();
            LoadData();
        }

        public void LoadData() {
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
