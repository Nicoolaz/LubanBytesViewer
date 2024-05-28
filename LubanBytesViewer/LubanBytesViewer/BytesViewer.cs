using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LubanBytesViewer
{

    public class GridDataInfo
    {
        public string tableName;
        public List<string> Headers;
        public List<List<string>> Rows;

        public GridDataInfo()
        {
            Headers = new List<string>();
            Rows = new List<List<string>>();
        }
    }

    public class SelectionInfo
    {
        public string TableName;
        public List<(int, int)> Cells;

        public SelectionInfo()
        {
            Cells = new List<(int, int)>();
        }
    }
    
    public partial class BytesViewer : Form
    {
        private string _selectedFolder;
        private TableDllImporter _dllInfos = new TableDllImporter();
        private Dictionary<string, SelectionInfo> _selectionInfos = new Dictionary<string, SelectionInfo>();
        private TableFileCollector _tableCollector;
        private string _currentSelectTable;
        private int _currentTabIndex;
        private int _searchFlag = 0;
        public BytesViewer()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*OpenFileDialog fileSelect = new OpenFileDialog();
            fileSelect.CheckFileExists = true;
            fileSelect.Multiselect = false;
            fileSelect.Filter = "鲁班二进制文件|*.bytes";
            fileSelect.FilterIndex = 0;
            if (fileSelect.ShowDialog() == DialogResult.OK)
            {
                _selectedFile = fileSelect.FileName;
                ShowData();
            }*/

            FolderBrowserDialog folderSelect = new FolderBrowserDialog();
            folderSelect.Description = "选择导表文件目录";
            if (folderSelect.ShowDialog() == DialogResult.OK)
            {
                _selectedFolder = folderSelect.SelectedPath;
                _selectionInfos.Clear();
                _tableCollector = new TableFileCollector(_selectedFolder, _dllInfos);
                //listView1.Visible = true;
                if(!string.IsNullOrEmpty(textBox1.Text)) RefreshSearchResult();
                RefreshListView();
                //BuildTableDict();
            }


            //throw new System.NotImplementedException();
        }

        private void RefreshListView()
        {
            listView1.Items.Clear();
            if (_tableCollector == null) return;
            
            if (!string.IsNullOrEmpty(textBox1.Text))
            {
                foreach (var selection in _selectionInfos.Values)
                {
                    listView1.Items.Add(selection.TableName);
                }
            }
            else
            {
                foreach (var dataGrid in _tableCollector.DataGrids)
                {
                    listView1.Items.Add(dataGrid.tableName);
                }
            }
            
        }
        
        private void ShowData(string nameKey)
        {
            var data = _tableCollector[nameKey];
            
            dataGridView1.ColumnCount = data.Headers.Count;
            for (int i = 0; i < data.Headers.Count; i++)
            {
                // var column = new DataGridViewTextBoxColumn();
                // column.Name = data.Headers[i];
                //column.CellType = typeof(string);
                dataGridView1.Columns[i].HeaderCell.Value = data.Headers[i];
            }

            dataGridView1.RowCount = data.Rows.Count;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                dataGridView1.Rows[i].SetValues(data.Rows[i].ToArray());
            }

            _currentTabIndex = 0;
            tabToSearchResult();
            //dataGridView1.AutoScrollOffset = new Point(200,1500);
            //dataGridView1.FirstDisplayedScrollingRowIndex = 15;
            
            //dataGridView1.Rows.Add(rows);

            //result = sb.ToString();
            //var generic = listProp.FieldType.GenericTypeArguments[0];



            //richTextBox1.Text = result;
        }

        private void tabToSearchResult()
        {
            if (string.IsNullOrEmpty(_currentSelectTable) || _currentTabIndex < 0 ||
                !_selectionInfos.ContainsKey(_currentSelectTable)) return;
            if (_currentTabIndex >= _selectionInfos[_currentSelectTable].Cells.Count)
                return;

            (int x, int y) = _selectionInfos[_currentSelectTable].Cells[_currentTabIndex];

            dataGridView1.CurrentCell = dataGridView1[x, y];
        }
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileSelect = new OpenFileDialog();
            fileSelect.CheckFileExists = true;
            fileSelect.Multiselect = false;
            fileSelect.Filter = "动态链接库文件|*.dll";
            fileSelect.FilterIndex = 0;
            if (fileSelect.ShowDialog() == DialogResult.OK)
            {

                if (!_dllInfos.ImportDll(fileSelect.FileName))
                {
                    //richTextBox1.Text = "Dll文件加载失败！！";
                    MessageBox.Show("Dll文件加载失败！！", "error", MessageBoxButtons.OK);
                    button1.Visible = false;
                }
                else
                {
                    //richTextBox1.Text = "Dll文件加载成功，请选择bytes文件进行查看~";
                    button1.Visible = true;
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //throw new System.NotImplementedException();
            if ((sender as ListView).SelectedItems.Count <= 0) return;
            _currentSelectTable = (sender as ListView).SelectedItems[0].Text;
            ShowData(_currentSelectTable);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            //throw new System.NotImplementedException();
            RefreshSearchResult();
            RefreshListView();
        }

        private void RefreshSearchResult()
        {
            var selections = _tableCollector.SearchWithStr(textBox1.Text, _searchFlag);
            _selectionInfos.Clear();
            foreach (var selection in selections)
            {
                _selectionInfos[selection.TableName] = selection;
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!_selectionInfos.ContainsKey(_currentSelectTable)) return;
            _currentTabIndex--;
            int max = _selectionInfos[_currentSelectTable].Cells.Count;
            if (_currentTabIndex < 0)
            {
                _currentTabIndex = max - Math.Abs(_currentTabIndex) % max;
            }
            tabToSearchResult();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!_selectionInfos.ContainsKey(_currentSelectTable)) return;
            _currentTabIndex++;
            int max = _selectionInfos[_currentSelectTable].Cells.Count;
            _currentTabIndex %= max;
            tabToSearchResult();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) _searchFlag |= (int)SearchFlag.ALLWORDS;
            else _searchFlag &= ~(int)SearchFlag.ALLWORDS;
            RefreshSearchResult();
            RefreshListView();
            //throw new System.NotImplementedException();
        }
    }
}