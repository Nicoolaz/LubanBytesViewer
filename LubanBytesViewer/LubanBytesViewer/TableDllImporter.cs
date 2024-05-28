using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace LubanBytesViewer
{
    public class TableDllImporter
    {
        private Dictionary<string, Type> typedict ;

        private Type ByteBufType;
        private ConstructorInfo ByteBufConstruct;

        public Type this[string tableName] => typedict.TryGetValue(tableName, out Type type) ? type : null;

        public object GetByteBuffIns(byte[] bytes)
        {
            if (ByteBufConstruct != null)
            {
                return ByteBufConstruct.Invoke(new object[] { bytes });
            }

            return null;
        }

        public ConstructorInfo GetTableConstruct(Type tableType)
        {
            return tableType.GetConstructor(new Type[] { ByteBufType });
        }
        
        public bool ImportDll(string _dllPath)
        {
            typedict = new Dictionary<string, Type>();
            try
            {
                Assembly ass = Assembly.LoadFile(_dllPath);
                var classes = ass.DefinedTypes.Where(t =>
                    t.IsClass
                ).ToList();
                foreach (var _c in classes)
                {
                    if (_c.Name == "ByteBuf")
                    {
                        ByteBufType = _c;
                        ByteBufConstruct = _c.GetConstructor(new Type[] { typeof(byte[]) });
                        continue;
                    }
                    
                    typedict.Add(_c.Name.ToLower(), _c);

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Fatal", MessageBoxButtons.OK);
            }

            if (typedict.Count <= 0)
            {
               
                return false;
            }

            return true;
        }
    }
}