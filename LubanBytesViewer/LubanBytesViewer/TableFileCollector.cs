using System.Collections.Generic;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LubanBytesViewer
{
    public enum SearchFlag
    {
        ALLWORDS = 1,
    }
    public class TableFileCollector
    {
        private List<GridDataInfo> dataGrids;
        private Dictionary<string, GridDataInfo> dataGridDict;
        //private int searchFlag = 0;
        public TableFileCollector(string dir, TableDllImporter _dllInfos)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            dataGridDict = new Dictionary<string, GridDataInfo>();
            List<Task<GridDataInfo>> tasks = dirInfo.GetFiles().Select((f)=> Task.Run(() =>
            {
                var path = f.FullName;
                string nameKey = Path.GetFileNameWithoutExtension(path);
                
                var _type = _dllInfos[nameKey];
                if (_type != null)
                {
                    var buf = _dllInfos.GetByteBuffIns(File.ReadAllBytes(path));
                    ConstructorInfo construct = _dllInfos.GetTableConstruct(_type);
                    if (construct != null)
                    {
                        var instance = construct.Invoke(new object[] { buf });
                        var listProp = instance.GetType()
                            .GetField("_dataList", BindingFlags.NonPublic | BindingFlags.Instance);
                        IList values = (IList)listProp.GetValue(instance);
                        return createGridData(values, nameKey);
                    }
                }
                return null;
            })).ToList();
            Task.WaitAll(tasks.ToArray());
            dataGrids = tasks.Where((t) => t.Result != null).Select((t) => t.Result).ToList();
            foreach (var data in dataGrids)
            {
                dataGridDict[data.tableName] = data;
            }
        }

        public GridDataInfo this[string tableName] => dataGridDict.TryGetValue(tableName, out GridDataInfo type) ? type : null;
        public List<GridDataInfo> DataGrids => dataGrids;
        public List<SelectionInfo> SearchWithStr(string searchStr, int searchFlag)
        {
            List<Task<SelectionInfo>> tasks = dataGrids.Select((g) => Task.Run(() =>
            {
                return searchTable(g, searchStr, searchFlag);
            })).ToList();

            Task.WaitAll(tasks.ToArray());
            var ret = tasks.Where((t) => t.Result.Cells.Count > 0).Select((t) => t.Result).ToList();
            return ret;
        }

        private GridDataInfo createGridData(IList rows, string tableName)
        {
            GridDataInfo ret = new GridDataInfo();
            ret.tableName = tableName;
            int count = rows.Count;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                    ret.Headers.AddRange(CreateColumnHeader(rows[i]));
                ret.Rows.Add(CreateRow(rows[i]));
            }

            return ret;
        }
        
        #region TableInfoCreate
        private List<string> CreateRow(object row)
        {
            List<string> ret = new List<string>();
            Type type = row.GetType();
            var fields = type.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if(fields[i].Name == "__ID__") continue;
                ret.Add(getValueStr(fields[i].GetValue(row)));
            }

            return ret;
        }
        private string[] CreateColumnHeader(object row)
        {
            Type type = row.GetType();
            var fields = type.GetFields();
            string[] ret = new string[fields.Length - 1];
            int index = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                if(fields[i].Name == "__ID__") continue;
                ret[index++] = fields[i].Name;
            }

            return ret;
        }

        private string keyValuePairToString(object pair)
        {
            Type objType = pair.GetType();
            FieldInfo key = objType.GetField("key");
            FieldInfo valueField = objType.GetField("value");
            if (key != null && valueField != null)
            {
                object key__ = key.GetValue(pair);
                object value__ = valueField.GetValue(pair);
                return $"{getValueStr(key__)} : {getValueStr(value__)}";
            }
            else
            {
                throw new ArgumentException("[keyValuePairToString Error]: object parameter is not a KeyValuePair!!");
            }
            
        }
        private string getValueStr(object value)
        {
            var sb = new StringBuilder(value.ToString());
            if (value is IDictionary)
            {
                sb.Clear();
                IEnumerator ittr = (value as IDictionary).GetEnumerator();
                if (!ittr.MoveNext()) sb.Append("{}");
                else
                {
                    // sb.Append($"{{ {(getValueStr((ittr.Current as KeyValuePair).Key))} ")
                    var obj = ittr.Current;
                    sb.Append($"{{ {keyValuePairToString(obj)}");
                    while (ittr.MoveNext())
                    {
                        sb.Append($", {keyValuePairToString(ittr.Current)}");
                    }
                    sb.Append("}");
                }
            }
            else if (value is IEnumerable && !(value is string))
            {
                sb.Clear();
                IEnumerator ittr = (value as IEnumerable).GetEnumerator();
                if(!ittr.MoveNext()) sb.Append("[]");
                else
                {
                    sb.Append($"[ {getValueStr(ittr.Current)}");
                
                    while (ittr.MoveNext())
                    {
                        sb.Append($", {getValueStr(ittr.Current)}");
                        //ittr.MoveNext();
                    }

                    sb.Append("]");
                }
                
            }

            return sb.ToString();
        }
        #endregion
        #region TableSearch

        private SelectionInfo searchTable(GridDataInfo tableGrid, string searchStr, int searchFlag)
        {
            SelectionInfo ret = new SelectionInfo();
            ret.TableName = tableGrid.tableName;
            for (int y = 0; y < tableGrid.Rows.Count; y++)
            {
                var row = tableGrid.Rows[y];
                for (int x = 0; x < row.Count; x++)
                {
                    var value = row[x];
                    if (searchExists(value, searchStr, searchFlag))
                    {
                        ret.Cells.Add((x, y));
                    }
                }
            }
            return ret;
        }

        private bool searchExists(string src, string searchStr, int searchFlag)
        {
            if ((searchFlag & (int)SearchFlag.ALLWORDS) > 0)
            {
                return src == searchStr;
            }
            else
            {
                return src.Contains(searchStr);
            }
        }
        #endregion
    }
}