using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NoIPClient
{
    internal class JsonFile<Type> where Type : class, new()
    {
        public Type Content { get; set; }
        public string LastPath { get => _lastPath; set => _lastPath = value; }

        private string _lastPath;

        public bool Load(string path)
        {
            string fileContent = null;
            try
            {
                if (File.Exists(path))
                    fileContent = File.ReadAllText(path);
                else
                    return false;
            }
            catch
            {
                return false;
            }

            try
            {
                Content = JsonConvert.DeserializeObject<Type>(fileContent);
            }
            catch 
            {
                return false;
            }

            _lastPath = path;

            return true;
        }

        public bool Save(string path)
        {
            return Save(path, Formatting.None);
        }
        public bool Save(string path, Formatting formatting)
        {
            if (Content == null)
                return false;
            
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(Content, formatting));
            }
            catch
            {
                return false;
            }

            _lastPath = path;

            return true;
        }
    }
}
