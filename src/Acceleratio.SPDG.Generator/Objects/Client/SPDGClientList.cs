﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.SharePoint.Client;

namespace Acceleratio.SPDG.Generator.Objects.Client
{
    internal class SPDGClientList : SPDGList
    {
        private readonly SPDGWeb _web;
        private readonly List _list;
        private readonly ClientContext _context;

        public override string Title
        {
            get { return _list.Title; }
        }

        public override string DefaultViewUrl
        {
            get { return _list.DefaultViewUrl; }
        }

        public override SPDGFolder RootFolder
        {
            get
            {
                return new SPDGClientFolder(_list.RootFolder, _context);
            }
        }

        private void invalidateFields()
        {
            _fields = null;
        }
        private List<SPDGField> _fields;
        public override IEnumerable<SPDGField> Fields
        {
            get
            {
                if (_fields == null)
                {
                    _fields=new List<SPDGField>();
                    _context.Load(_list.Fields, coll => coll.Include(x => x.Title, x => x.StaticName, x => x.InternalName));
                    _context.ExecuteQuery();
                    foreach (var field in _list.Fields)
                    {
                        _fields.Add(new SPDGField(field.Title, field.InternalName));
                    }
                }
                return _fields;
            }
        }

        public SPDGClientList(SPDGWeb web, List list, ClientContext context)
        {
            _web = web;
            _list = list;
            _context = context;
        }

        public static Expression<Func<List, object>>[] IncludeExpression
        {
            get
            {
                List<Expression<Func<List, object>>> includeExpression = new List<Expression<Func<List, object>>>();
                includeExpression.Add(web => web.Id);
                includeExpression.Add(web => web.Title);
                includeExpression.Add(web => web.DefaultViewUrl);
                includeExpression.Add(web => web.RootFolder);
                return includeExpression.ToArray();
            }
        }
        

        public override void AddFields(IEnumerable<SPDGFieldInfo> fields, bool addToDefaultView)
        {            
            invalidateFields();

            string schemaFormat = "<Field Type='{0}' DisplayName='{1}'/>";
            var addFieldOptions = AddFieldOptions.DefaultValue;
            if (addToDefaultView)
            {
                addFieldOptions |= AddFieldOptions.AddFieldToDefaultView;
            }
            foreach (var fieldInfo in fields)
            {
                _list.Fields.AddFieldAsXml(string.Format(schemaFormat, fieldInfo.FieldType, fieldInfo.DisplayName), false, addFieldOptions );
            }   
            _list.Update();
            _context.ExecuteQuery();
        }

        public override void AddItems(IEnumerable<ISPDGListItemInfo> items)
        {
            int counter = 0;
            foreach (var itemInfo in items)
            {
                counter++;
                var itemCreationInfo=new ListItemCreationInformation();
                var item=_list.AddItem(itemCreationInfo);
                foreach (var field in itemInfo.GetAvailableFields())
                {
                    item[field] = itemInfo[field];
                }
                item.Update();
                if (counter >=500)
                {
                    counter = 0;
                    _context.ExecuteQuery();
                }
            }
            _context.ExecuteQuery();
        }
    }
}
