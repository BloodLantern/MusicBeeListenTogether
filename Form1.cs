using System.Windows.Forms;

namespace MusicBeePlugin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Disable button1 if we are already connected

            // Initialize treeView1 contents with the currently connected users and select the current queue owner (if applicable)
            treeView1.ExpandAll();
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            // Connect to the server. If it fails, leave the button like this. If it works, disable it.
        }

        private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Go duoQ with root node
        }

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // Cancel selection and instead select root node
            TreeNode node = e.Node;
            TreeNode parent = node.Parent;
            
            if (parent == null)
                return;
            
            e.Cancel = true;

            treeView1.SelectedNode = parent;
        }
    }
}

