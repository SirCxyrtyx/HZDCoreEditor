using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HZDCoreEditorUI.Util;
using Utility;

namespace HZDCoreEditorUI.UI
{
    public sealed class TreeDataHashMapNode : TreeDataNode
    {
        public override object Value => $"{TypeName} ({GetListLength()})";

        public override bool HasChildren => GetListLength() > 0;
        public override List<TreeDataNode> Children { get; }
        public override bool IsEditable => false;

        private readonly FieldOrProperty ParentFieldEntry;

        public TreeDataHashMapNode(object parent, FieldOrProperty member, NodeAttributes attributes)
            : base(parent)
        {
            Name = member.GetName();
            TypeName = member.GetMemberType().GetFriendlyName();

            Children = new List<TreeDataNode>();
            ParentFieldEntry = member;

            if (!attributes.HasFlag(NodeAttributes.HideChildren))
            {
                Children = new List<TreeDataNode>();
                AddListChildren();
            }
        }

        private IDictionary GetDictionary()
        {
            return ParentFieldEntry.GetValue<IDictionary>(ParentObject);
        }

        private int GetListLength()
        {
            return GetDictionary()?.Count ?? 0;
        }

        private void AddListChildren()
        {
            var asDictionary = GetDictionary();

            if (asDictionary != null)
            {
                // Fetch the type of TValue from IDictionary<T>
                var enumerableTemplateType = asDictionary.GetType()
                    .GetInterfaces()
                    .Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 2)
                    .Single(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    .GenericTypeArguments[1];

                foreach (uint key in asDictionary.Keys)
                {
                    Children.Add(new TreeDataHashMapKvpNode(asDictionary, key, enumerableTemplateType));
                }
            }
        }
    }

    public sealed class TreeDataHashMapKvpNode : TreeDataNode
    {
        public override object Value => ObjectWrapper;

        public override bool HasChildren => ObjectWrapperNode.HasChildren;
        public override List<TreeDataNode> Children => ObjectWrapperNode.Children;
        public override bool IsEditable => ObjectWrapperNode.IsEditable;

        private readonly uint ParentDictionaryKey;
        private TreeDataNode ObjectWrapperNode;

        // Property is needed in order to get a FieldOrProperty handle
        private object ObjectWrapper
        {
            get => ((IDictionary)ParentObject)[ParentDictionaryKey];
            set => ((IDictionary)ParentObject)[ParentDictionaryKey] = value;
        }

        public TreeDataHashMapKvpNode(IDictionary parent, uint key, Type elementType)
            : base(parent)
        {
            Name = $"[{key}]";
            TypeName = elementType.GetFriendlyName();

            ParentDictionaryKey = key;

            AddObjectChildren(elementType);
        }

        private void AddObjectChildren(Type elementType)
        {
            ObjectWrapperNode = CreateNode(this, new FieldOrProperty(GetType(), nameof(ObjectWrapper)), elementType);
        }
    }
}
