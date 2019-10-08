using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EDMXTrimmer
{
    class EdmxTrimmer
    {
        public string EdmxFile { get; private set; }
        public bool Verbose { get; private set; }
        public List<string> EntitiesToKeep { get; private set; }
        public string OutputFileName { get; set; }

        private XmlDocument _xmlDocument;
        private const string ENTITY_TYPE = "EntityType";
        private const string ENTITY_SET = "EntitySet";
        private const string NAVIGATION_PROPERTY = "NavigationProperty";
        private const string ACTION = "Action";
        private const string ATTRIBUTE_NAME = "Name";
        private const string ATTRIBUTE_TYPE = "Type";
        private const string ENTITYNAMESPACE = "Microsoft.Dynamics.DataEntities.";

        public EdmxTrimmer(string edmxFile, string outputFileName,  bool verbose = true, List<String> entitiesToKeep = null)
        {
            this.EdmxFile = edmxFile;
            this.Verbose = verbose;
            this.OutputFileName = outputFileName;

            this.EntitiesToKeep = new List<string>();
            if(entitiesToKeep != null && entitiesToKeep.Count > 0)
            {
                this.EntitiesToKeep.AddRange(entitiesToKeep);
            }

            this.LoadFile();
        }

        private void LoadFile()
        {
            this._xmlDocument = new XmlDocument();
            this._xmlDocument.Load(this.EdmxFile);
        }

        public void AnalyzeFile()
        {
            var entitySets = this._xmlDocument.GetElementsByTagName(ENTITY_SET).Cast<XmlNode>().ToList();
            var entityTypes = this._xmlDocument.GetElementsByTagName(ENTITY_TYPE).Cast<XmlNode>().ToList();
            var entityActions = this._xmlDocument.GetElementsByTagName(ACTION).Cast<XmlNode>().ToList();

            List<String> entityTypesFound = new List<string>();
            //if (this.Verbose)
            //{
            //    // Print list of ALL entities
            //    entitySets.ForEach(n => Console.WriteLine(n.Attributes[ATTRIBUTE_NAME].Value));
            //}

            var entitiesKeep = entitySets.Where(n => this.EntitiesToKeep.Contains(n.Attributes[ATTRIBUTE_NAME].Value)).ToList();
            entitiesKeep.ForEach(n =>
            {
                string entityType = n.Attributes[ENTITY_TYPE].Value;
                entityType = entityType.Replace(ENTITYNAMESPACE, "");
                entityTypesFound.Add(entityType);
            });

            if (this.Verbose)
            {
                Console.WriteLine("Entity definitions found:");
                entityTypesFound.ForEach(n => Console.WriteLine(n));
            }

            // Remove entities not required (EntitySet)
            entitySets.Except(entitiesKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));
            //Remove unwanted Nodes in the Entity Set
            entitiesKeep.ForEach(n =>
            {
                // Remove Node NavigationProperty
                var navProperties = n.ChildNodes.Cast<XmlNode>()
                    .Where(navProp => navProp.Name.Equals(NAVIGATION_PROPERTY)).ToList();
                navProperties
                    .ForEach(navProp => navProp.ParentNode.RemoveChild(navProp));
            });
            
            // Remove all navigation properties

            this._xmlDocument.GetElementsByTagName(NAVIGATION_PROPERTY).Cast<XmlNode>()
                .Where(navProp => !entityTypesFound.Any(entityType => Regex.IsMatch("collection(Microsoft.Dynamics.DataEntities.ReleasedProductsV2)", ENTITYNAMESPACE + entityType + "\\)?$"))).ToList()
                .ForEach(n => n.ParentNode.RemoveChild(n));

            // Remove entity not required (EntityType)
            var entityTypesKeep = entityTypes.Where(n => entityTypesFound.Contains(n.Attributes[ATTRIBUTE_NAME].Value)).ToList();
            entityTypes.Except(entityTypesKeep).ToList().ForEach(n => n.ParentNode.RemoveChild(n));

            // Remove all Actions
            entityActions.ForEach(n => n.ParentNode.RemoveChild(n));

            this._xmlDocument.Save(OutputFileName);
            if(this.Verbose)
            {
                Console.WriteLine($"EDMX Saved to file: {OutputFileName}");
            }

        }
    }
}
