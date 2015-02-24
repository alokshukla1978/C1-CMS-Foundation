using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using Composite.C1Console.Forms;
using Composite.C1Console.Security;
using Composite.Core.Extensions;
using Composite.Core.ResourceSystem;
using Composite.Core.Types;
using Composite.Core.Xml;
using Composite.Data.DynamicTypes.Foundation;
using Composite.Data.ProcessControlled;
using Composite.Data.ProcessControlled.ProcessControllers.GenericPublishProcessController;
using Composite.Data.Types;
using Composite.Data.Validation;
using Composite.Data.Validation.ClientValidationRules;
using Composite.Functions;
using Composite.Functions.Foundation;

using Texts = Composite.Core.ResourceSystem.LocalizationFiles.Composite_Plugins_GeneratedDataTypesElementProvider;

namespace Composite.Data.DynamicTypes
{
    /// <summary>    
    /// </summary>
    /// <exclude />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class DataTypeDescriptorFormsHelper
    {
        private readonly DataTypeDescriptor _dataTypeDescriptor;
        private readonly List<string> _readOnlyFields = new List<string>();
        private readonly bool _showPublicationStatusSelector;
        private readonly string _bindingNamesPrefix;

        private XDocument _customFormDefinition;
        private bool _customFormDefinitionInitialized;
        private string _generatedForm;
        private XElement _bindingsXml;
        private XElement _panelXml;

        private const string PublicationStatusPostFixBindingName = "___PublicationStatus___";
        private const string PublicationStatusOptionsPostFixBindingName = "___PublicationStatusOptions___";

        private static readonly XElement CmsFormElementTemplate;
        private static readonly XElement CmsBindingsElementTemplate;
        private static readonly XElement CmsLayoutElementTemplate;


        /// <exclude />
        static DataTypeDescriptorFormsHelper()
        {
            CmsFormElementTemplate = XElement.Parse(string.Format(@"<cms:{0} xmlns:cms=""{1}"" xmlns=""{2}"" xmlns:ff=""{3}"" xmlns:f=""{4}"" />", FormKeyTagNames.FormDefinition, Namespaces.BindingForms10, Namespaces.BindingFormsStdUiControls10, Namespaces.BindingFormsStdFuncLib10, FunctionTreeConfigurationNames.NamespaceName));
            CmsBindingsElementTemplate = new XElement(Namespaces.BindingForms10 + FormKeyTagNames.Bindings);
            CmsLayoutElementTemplate = new XElement(Namespaces.BindingForms10 + FormKeyTagNames.Layout);
        }


        /// <exclude />
        public DataTypeDescriptorFormsHelper(DataTypeDescriptor dataTypeDescriptor, bool showPublicationStatusSelector, EntityToken entityToken)
            : this(dataTypeDescriptor, null, showPublicationStatusSelector, entityToken)
        {
        }


        /// <exclude />
        public DataTypeDescriptorFormsHelper(DataTypeDescriptor dataTypeDescriptor)
            : this(dataTypeDescriptor, null, false, null)
        {
        }


        /// <exclude />
        public DataTypeDescriptorFormsHelper(DataTypeDescriptor dataTypeDescriptor, string bindingNamesPrefix)
            : this(dataTypeDescriptor, bindingNamesPrefix, false, null)
        {
        }


        /// <exclude />
        public DataTypeDescriptorFormsHelper(DataTypeDescriptor dataTypeDescriptor, string bindingNamesPrefix, bool showPublicationStatusSelector, EntityToken entityToken)
        {
            if (dataTypeDescriptor == null) throw new ArgumentNullException("dataTypeDescriptor");

            _dataTypeDescriptor = dataTypeDescriptor;
            _bindingNamesPrefix = bindingNamesPrefix;
            _showPublicationStatusSelector = showPublicationStatusSelector;
            EntityToken = entityToken;
            LayoutIconHandle = null;
        }


        /// <exclude />
        public DataTypeDescriptor DataTypeDescriptor
        {
            get
            {
                return _dataTypeDescriptor;
            }
        }


        /// <exclude />
        public XDocument CustomFormDefinition
        {
            get
            {
                if (!_customFormDefinitionInitialized)
                {
                    _customFormDefinition = DynamicTypesCustomFormFacade.GetCustomFormMarkup(_dataTypeDescriptor);

                    _customFormDefinitionInitialized = true;
                }

                return _customFormDefinition;
            }
            set
            {
                _customFormDefinition = value;
                _customFormDefinitionInitialized = true;
            }
        }

        /// <exclude />
        public void AddReadOnlyField(string fieldName)
        {
            _readOnlyFields.Add(fieldName);
        }



        /// <exclude />
        public void AddReadOnlyFields(IEnumerable<string> fieldNames)
        {
            _readOnlyFields.AddRange(fieldNames);
        }



        /// <exclude />
        public string GetForm()
        {
            if (_generatedForm == null)
            {
                GenerateForm();
            }

            return _generatedForm;
        }



        /// <exclude />
        public XElement BindingXml
        {
            get
            {
                if (_bindingsXml == null)
                {
                    GenerateForm();
                }

                return _bindingsXml;
            }
        }



        /// <exclude />
        public XElement PanelXml
        {
            get
            {
                if (_panelXml == null)
                {
                    GenerateForm();
                }

                return _panelXml;
            }
        }



        /// <exclude />
        public string BindingNamesPrefix
        {
            get
            {
                return _bindingNamesPrefix;
            }
        }



        /// <exclude />
        public void UpdateWithNewBindings(Dictionary<string, object> bindings)
        {
            var newBindigns = GetNewBindings();

            foreach (var kvp in newBindigns)
            {
                bindings[kvp.Key] = kvp.Value;
            }
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(decimal)) return (decimal)0.0;
            if (type == typeof(DateTime)) return DateTime.Now;
            if (type == typeof(bool)) return false;
            if (type == typeof(Guid)) return Guid.Empty;

            return null;
        }

        /// <exclude />
        public Dictionary<string, object> GetNewBindings()
        {
            var newBindings = new Dictionary<string, object>();

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                var fieldType = fieldDescriptor.InstanceType;

                object value;
                if (fieldDescriptor.IsNullable
                    || (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    value = null;
                }
                else
                {
                    if (fieldType == typeof(string) && fieldDescriptor.ForeignKeyReferenceTypeName == null)
                    {
                        value = "";
                    }
                    else
                    {
                        value = GetDefaultValue(fieldType);
                    }
                }

                newBindings.Add(GetBindingName(fieldDescriptor), value);
            }


            //TODO: This code is dublicated. /MRJ
            if (_showPublicationStatusSelector &&
                _dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
            {
                newBindings.Add(PublicationStatusBindingName, GenericPublishProcessController.Draft);

                IDictionary<string, string> transitionNames = new Dictionary<string, string>();
                transitionNames.Add(GenericPublishProcessController.Draft, StringResourceSystemFacade.GetString("Composite.Plugins.GeneratedDataTypesElementProvider", "DraftTransition"));
                transitionNames.Add(GenericPublishProcessController.AwaitingApproval, StringResourceSystemFacade.GetString("Composite.Plugins.GeneratedDataTypesElementProvider", "AwaitingApprovalTransition"));

                var username = UserValidationFacade.GetUsername();
                var userPermissionDefinitions = PermissionTypeFacade.GetUserPermissionDefinitions(username);
                var userGroupPermissionDefinition = PermissionTypeFacade.GetUserGroupPermissionDefinitions(username);
                var currentPermissionTypes = PermissionTypeFacade.GetCurrentPermissionTypes(UserValidationFacade.GetUserToken(), EntityToken, userPermissionDefinitions, userGroupPermissionDefinition);
                foreach (var permissionType in currentPermissionTypes)
                {
                    if (GenericPublishProcessController.AwaitingPublicationActionPermissionType.Contains(permissionType))
                    {
                        transitionNames.Add(GenericPublishProcessController.AwaitingPublication,
                            LocalizationFiles.Composite_Management.Website_Forms_Administrative_EditPage_AwaitingPublicationTransition);
                        break;
                    }
                }

                newBindings.Add(PublicationStatusOptionsBindingName, transitionNames);
            }


            return newBindings;
        }



        /// <exclude />
        public void UpdateWithBindings(IData dataObject, Dictionary<string, object> bindings)
        {
            var newBindigns = GetBindings(dataObject);

            foreach (var kvp in newBindigns)
            {
                bindings[kvp.Key] = kvp.Value;
            }
        }



        /// <exclude />
        public Dictionary<string, object> GetBindings(IData dataObject)
        {
            return GetBindings(dataObject, false);
        }



        /// <exclude />
        public Dictionary<string, object> GetBindings(IData dataObject, bool allowMandatoryNonDefaultingProperties)
        {
            if (dataObject == null) throw new ArgumentNullException("dataObject");

            var bindings = new Dictionary<string, object>();

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                var propertyInfo = dataObject.GetType().GetProperty(fieldDescriptor.Name);

                if (propertyInfo.CanRead)
                {
                    var value = propertyInfo.GetGetMethod().Invoke(dataObject, null);

                    if (value == null && !fieldDescriptor.IsNullable)
                    {
                        if (fieldDescriptor.IsNullable)
                        {
                            // Ignore, null is allowed
                        }
                        else if (fieldDescriptor.InstanceType.IsGenericType
                                 && fieldDescriptor.InstanceType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            // Ignore, null is allowed
                        }
                        else if (allowMandatoryNonDefaultingProperties)
                        {
                            if (propertyInfo.PropertyType == typeof(string) && fieldDescriptor.ForeignKeyReferenceTypeName == null) //FK fields stay NULL
                            {
                                value = "";
                            }
                            else
                            {
                                value = GetDefaultValue(propertyInfo.PropertyType);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException(string.Format("Field '{0}' on type '{1}' is null, does not allow null and does not have a default value", fieldDescriptor.Name, _dataTypeDescriptor.TypeManagerTypeName));
                        }
                    }

                    bindings.Add(GetBindingName(fieldDescriptor), value);
                }
            }

            if (_showPublicationStatusSelector &&
                _dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
            {
                bindings.Add(PublicationStatusBindingName, ((IPublishControlled)dataObject).PublicationStatus);

                IDictionary<string, string> transitionNames = new Dictionary<string, string>();
                transitionNames.Add(GenericPublishProcessController.Draft, Texts.DraftTransition);
                transitionNames.Add(GenericPublishProcessController.AwaitingApproval, Texts.AwaitingApprovalTransition);

                var username = UserValidationFacade.GetUsername();
                var userPermissionDefinitions = PermissionTypeFacade.GetUserPermissionDefinitions(username);
                var userGroupPermissionDefinition = PermissionTypeFacade.GetUserGroupPermissionDefinitions(username);
                var currentPermissionTypes = PermissionTypeFacade.GetCurrentPermissionTypes(UserValidationFacade.GetUserToken(), EntityToken, userPermissionDefinitions, userGroupPermissionDefinition);
                foreach (var permissionType in currentPermissionTypes)
                {
                    if (GenericPublishProcessController.AwaitingPublicationActionPermissionType.Contains(permissionType))
                    {
                        transitionNames.Add(GenericPublishProcessController.AwaitingPublication,
                            LocalizationFiles.Composite_Management.Website_Forms_Administrative_EditPage_AwaitingPublicationTransition);
                        break;
                    }
                }

                bindings.Add(PublicationStatusOptionsBindingName, transitionNames);

                var existingPublishSchedule =
                            (from ps in DataFacade.GetData<IPublishSchedule>()
                             where ps.DataType == dataObject.DataSourceId.InterfaceType.FullName &&
                             ps.DataId == dataObject.GetUniqueKey().ToString()
                             select ps).FirstOrDefault();

                if (existingPublishSchedule != null)
                {
                    bindings.Add("PublishDate", existingPublishSchedule.PublishDate);
                }
                else
                {
                    bindings.Add("PublishDate", null);
                }

                var existingUnpublishSchedule =
                                (from ps in DataFacade.GetData<IUnpublishSchedule>()
                                 where ps.DataType == dataObject.DataSourceId.InterfaceType.FullName &&
                                    ps.DataId == dataObject.GetUniqueKey().ToString()
                                 select ps).FirstOrDefault();

                if (existingUnpublishSchedule != null)
                {
                    bindings.Add("UnpublishDate", existingUnpublishSchedule.UnpublishDate);
                }
                else
                {
                    bindings.Add("UnpublishDate", null);
                }
            }

            return bindings;
        }


        /// <exclude />
        public Dictionary<string, List<ClientValidationRule>> GetBindingsValidationRules(IData data)
        {
            Verify.ArgumentNotNull(data, "data");

            var result = new Dictionary<string, List<ClientValidationRule>>();

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                var rules = ClientValidationRuleFacade.GetClientValidationRules(data, fieldDescriptor.Name);

                result.Add(GetBindingName(fieldDescriptor), rules);
            }

            return result;
        }



        /// <exclude />
        public Dictionary<string, string> BindingsToObject(Dictionary<string, object> bindings, IData dataObject)
        {
            var errorMessages = new Dictionary<string, string>();

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                if (_readOnlyFields.Contains(fieldDescriptor.Name))
                {
                    continue;
                }

                var bindingName = GetBindingName(fieldDescriptor);

                if (!bindings.ContainsKey(bindingName))
                {
                    Verify.That(fieldDescriptor.IsNullable, "Missing value for field '{0}'", fieldDescriptor.Name);
                    continue;
                }

                var propertyInfo = dataObject.GetType().GetProperty(fieldDescriptor.Name);

                if (propertyInfo.CanWrite)
                {
                    var newValue = bindings[bindingName];

                    if (newValue is string && (newValue as string) == "" && IsNullableStringReference(propertyInfo))
                    {
                        newValue = null;
                    }

                    try
                    {
                        newValue = ValueTypeConverter.Convert(newValue, propertyInfo.PropertyType);

                        propertyInfo.GetSetMethod().Invoke(dataObject, new[] { newValue });
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add(bindingName, ex.Message);
                    }
                }
            }

            if (_showPublicationStatusSelector &&
                _dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
            {
                var publishControlled = dataObject as IPublishControlled;

                publishControlled.PublicationStatus = (string)bindings[PublicationStatusBindingName];
            }

            if (errorMessages.Count > 0)
            {
                return errorMessages;
            }

            return null;
        }


        private static bool IsNullableStringReference(PropertyInfo propertyInfo)
        {
            var dataType = propertyInfo.DeclaringType;
            return DataAttributeFacade.GetDataReferenceProperties(dataType)
                                      .Any(foreignKey => foreignKey.SourcePropertyName == propertyInfo.Name && foreignKey.IsNullableString);
        }


        /// <exclude />
        public Dictionary<string, string> ObjectToBindings(IData dataObject, Dictionary<string, object> bindings)
        {
            var errorMessages = new Dictionary<string, string>();

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                var bindingName = GetBindingName(fieldDescriptor);

                if (bindings.ContainsKey(bindingName))
                {
                    var propertyInfo = dataObject.GetType().GetProperty(fieldDescriptor.Name);

                    Verify.IsNotNull(propertyInfo, "Missing property type '{0}' does not contain property '{1}'", dataObject.GetType(), fieldDescriptor.Name);

                    if (propertyInfo.CanRead)
                    {
                        var newValue = propertyInfo.GetValue(dataObject, null);

                        if (newValue == null && !fieldDescriptor.IsNullable)
                        {
                            var fieldType = fieldDescriptor.InstanceType;

                            if (fieldType == typeof(string) && fieldDescriptor.ForeignKeyReferenceTypeName == null)
                            {
                                newValue = "";
                            }
                            else
                            {
                                newValue = GetDefaultValue(fieldType);
                            }
                        }

                        try
                        {
                            bindings[bindingName] = newValue;
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add(bindingName, ex.Message);
                        }
                    }
                }
            }

            if (_showPublicationStatusSelector &&
                _dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
            {
                var publishControlled = dataObject as IPublishControlled;

                bindings[PublicationStatusBindingName] = publishControlled.PublicationStatus;
            }

            return errorMessages.Count > 0 ? errorMessages : null;
        }



        /// <exclude />
        public string LayoutIconHandle
        {
            get;
            set;
        }



        /// <exclude />
        public static XNamespace MainNamespace
        {
            get { return Namespaces.BindingFormsStdUiControls10; }
        }



        /// <exclude />
        public static XNamespace CmsNamespace
        {
            get { return Namespaces.BindingForms10; }
        }



        /// <exclude />
        public static XNamespace FunctionNamespace
        {
            get { return Namespaces.BindingFormsStdFuncLib10; }
        }



        /// <exclude />
        public string FieldGroupLabel
        {
            get;
            set;
        }



        /// <exclude />
        public string LayoutLabel
        {
            get;
            set;
        }


        private Type GetFieldBindingType(DataFieldDescriptor fieldDescriptor)
        {
            var bindingType = fieldDescriptor.InstanceType;

            // Nullable<T> handling. Allowed types: Nullable<Guid>, Nullable<int>, Nullable<decimal>
            if (bindingType != typeof(Guid?)
                && bindingType != typeof(int?)
                && bindingType != typeof(decimal?)
                && bindingType.IsGenericType && bindingType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return bindingType.GetGenericArguments()[0];
            }

            return bindingType;
        }


        private EntityToken EntityToken
        {
            get;
            set;
        }


        private void GenerateForm()
        {
            var fieldNameToBindingNameMapper = new Dictionary<string, string>();

            _bindingsXml = new XElement(CmsBindingsElementTemplate);
            var layout = new XElement(CmsLayoutElementTemplate);

            if (!string.IsNullOrEmpty(LayoutIconHandle))
            {
                layout.Add(new XAttribute("iconhandle", LayoutIconHandle));
            }

            // Add a read binding as the layout label
            if (!string.IsNullOrEmpty(LayoutLabel))
            {
                var labelAttribute = new XAttribute("label", LayoutLabel);
                layout.Add(labelAttribute);
            }
            else if (!string.IsNullOrEmpty(_dataTypeDescriptor.LabelFieldName))
            {
                layout.Add((new XElement(CmsNamespace + "layout.label", new XElement(CmsNamespace + "read", new XAttribute("source", _dataTypeDescriptor.LabelFieldName)))));
            }


            _panelXml = new XElement(MainNamespace + "FieldGroup");
            if (!string.IsNullOrEmpty(FieldGroupLabel))
            {
                _panelXml.Add(new XAttribute("Label", FieldGroupLabel));
            }

            layout.Add(_panelXml);

            foreach (var fieldDescriptor in _dataTypeDescriptor.Fields)
            {
                var bindingType = GetFieldBindingType(fieldDescriptor);
                var bindingName = GetBindingName(fieldDescriptor);

                fieldNameToBindingNameMapper.Add(fieldDescriptor.Name, bindingName);

                var binding = new XElement(CmsNamespace + FormKeyTagNames.Binding,
                    new XAttribute("name", bindingName),
                    new XAttribute("type", bindingType));

                if (fieldDescriptor.IsNullable)
                {
                    binding.Add(new XAttribute("optional", "true"));
                }

                _bindingsXml.Add(binding);

                if (!_readOnlyFields.Contains(fieldDescriptor.Name))
                {
                    XElement widgetFunctionMarkup;
                    var label = fieldDescriptor.FormRenderingProfile.Label;
                    if (label.IsNullOrEmpty())
                    {
                        label = fieldDescriptor.Name;
                    }

                    var helptext = fieldDescriptor.FormRenderingProfile.HelpText ?? "";

                    if (!string.IsNullOrEmpty(fieldDescriptor.FormRenderingProfile.WidgetFunctionMarkup))
                    {
                        widgetFunctionMarkup = XElement.Parse(fieldDescriptor.FormRenderingProfile.WidgetFunctionMarkup);
                    }
                    else if (!DataTypeDescriptor.IsCodeGenerated && fieldDescriptor.FormRenderingProfile.WidgetFunctionMarkup == null)
                    {
                        // Auto generating a widget for not code generated data types
                        Type fieldType;

                        if (!fieldDescriptor.ForeignKeyReferenceTypeName.IsNullOrEmpty())
                        {
                            Type foreignKeyType;

                            try
                            {
                                foreignKeyType = Type.GetType(fieldDescriptor.ForeignKeyReferenceTypeName, true);
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException("Failed to get referenced foreign key type '{0}'".FormatWith(fieldDescriptor.ForeignKeyReferenceTypeName), ex);
                            }

                            var referenceTemplateType = fieldDescriptor.IsNullable ? typeof(NullableDataReference<>) : typeof(DataReference<>);

                            fieldType = referenceTemplateType.MakeGenericType(foreignKeyType);
                        }
                        else
                        {
                            fieldType = fieldDescriptor.InstanceType;
                        }

                        var widgetFunctionProvider = StandardWidgetFunctions.GetDefaultWidgetFunctionProviderByType(fieldType);
                        if (widgetFunctionProvider != null)
                        {
                            widgetFunctionMarkup = widgetFunctionProvider.SerializedWidgetFunction;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var widgetRuntimeTreeNode = (WidgetFunctionRuntimeTreeNode)FunctionTreeBuilder.Build(widgetFunctionMarkup);
                    widgetRuntimeTreeNode.Label = label;
                    widgetRuntimeTreeNode.HelpDefinition = new HelpDefinition(helptext);
                    widgetRuntimeTreeNode.BindingSourceName = bindingName;

                    var element = (XElement)widgetRuntimeTreeNode.GetValue();
                    _panelXml.Add(element);
                }
            }

            if (_dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
            {
                var placeholder = new XElement(MainNamespace + "PlaceHolder");
                _panelXml.Remove();

                placeholder.Add(_panelXml);
                layout.Add(placeholder);
                
                var publishFieldsXml = new XElement(MainNamespace + "FieldGroup", new XAttribute("Label", "Publication settings")); // TODO: localize!
                placeholder.Add(publishFieldsXml);
                

                if (!_dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPageMetaData)))
                {
                    var publishDateBinding = new XElement(CmsNamespace + FormKeyTagNames.Binding,
                        new XAttribute("name", "PublishDate"),
                        new XAttribute("type", typeof(DateTime)),
                        new XAttribute("optional", "true"));

                    _bindingsXml.Add(publishDateBinding);

                    publishFieldsXml.Add(
                        new XElement(MainNamespace + "DateSelector",
                            new XAttribute("Label", "Publish date"), // TODO: localize and add help text
                            new XElement(MainNamespace + "DateSelector.Date",
                                new XElement(CmsNamespace + "bind",
                                    new XAttribute("source", "PublishDate")))));

                    var unpublishDateBinding = new XElement(CmsNamespace + FormKeyTagNames.Binding,
                        new XAttribute("name", "UnpublishDate"),
                        new XAttribute("type", typeof(DateTime)),
                        new XAttribute("optional", "true"));

                    _bindingsXml.Add(unpublishDateBinding);

                    publishFieldsXml.Add(
                        new XElement(MainNamespace + "DateSelector",
                            new XAttribute("Label", "Unpublish date"), // TODO: localize and add help text
                            new XElement(MainNamespace + "DateSelector.Date",
                                new XElement(CmsNamespace + "bind",
                                    new XAttribute("source", "UnpublishDate")))));
                }

                if (_showPublicationStatusSelector)
                {
                    var publicationStatusBinding = new XElement(CmsNamespace + FormKeyTagNames.Binding,
                        new XAttribute("name", PublicationStatusBindingName),
                        new XAttribute("type", typeof(string)));

                    _bindingsXml.Add(publicationStatusBinding);

                    var publicationStatusOptionsBinding = new XElement(CmsNamespace + FormKeyTagNames.Binding,
                        new XAttribute("name", PublicationStatusOptionsBindingName),
                        new XAttribute("type", typeof(object)));

                    _bindingsXml.Add(publicationStatusOptionsBinding);

                    var element =
                        new XElement(MainNamespace + "KeySelector",
                            new XAttribute("OptionsKeyField", "Key"),
                            new XAttribute("OptionsLabelField", "Value"),
                            new XAttribute("Label", "${Composite.Plugins.GeneratedDataTypesElementProvider, LabelPublicationState}"),
                            new XElement(MainNamespace + "KeySelector.Selected",
                                new XElement(CmsNamespace + "bind",
                                    new XAttribute("source", PublicationStatusBindingName)
                                )
                            ),
                            new XElement(MainNamespace + "KeySelector.Options",
                                new XElement(CmsNamespace + "read",
                                    new XAttribute("source", PublicationStatusOptionsBindingName)
                                )
                            )
                        );


                    publishFieldsXml.Add(element);
                }
            }

            var formDefinition = new XElement(CmsFormElementTemplate);
            formDefinition.Add(_bindingsXml);
            formDefinition.Add(layout);

            if (CustomFormDefinition == null)
            {
                _generatedForm = formDefinition.ToString();
            }
            else
            {
                if (_dataTypeDescriptor.SuperInterfaces.Contains(typeof(IPublishControlled)))
                {
                    fieldNameToBindingNameMapper.Add("PublishDate", "PublishDate");
                    fieldNameToBindingNameMapper.Add("UnpublishDate", "UnpublishDate");
                }

                Func<XElement, IEnumerable<XAttribute>> getBindingsFunc =
                    doc => doc.Descendants(CmsNamespace + "binding").Attributes("name")
                           .Concat(doc.Descendants(CmsNamespace + "bind").Attributes("source"))
                           .Concat(doc.Descendants(CmsNamespace + "read").Attributes("source"));

                // Validation
                foreach (var bindingNameAttribute in getBindingsFunc(CustomFormDefinition.Root))
                {
                    var bindingName = bindingNameAttribute.Value;

                    if (!fieldNameToBindingNameMapper.ContainsKey(bindingName))
                    {
                        throw new ParseDefinitionFileException("Invalid binding name '{0}'".FormatWith(bindingName), bindingNameAttribute);
                    }
                }

                var formDefinitionElement = new XElement(CustomFormDefinition.Root);

                foreach (var bindingNameAttribute in getBindingsFunc(formDefinitionElement))
                {
                    bindingNameAttribute.Value = fieldNameToBindingNameMapper[bindingNameAttribute.Value];
                }

                if (!string.IsNullOrEmpty(FieldGroupLabel))
                {
                    foreach (var fieldGroupElement in formDefinitionElement.Descendants(MainNamespace + "FieldGroup"))
                    {
                        if (fieldGroupElement.Attribute("Label") == null)
                        {
                            fieldGroupElement.Add(new XAttribute("Label", FieldGroupLabel));
                        }
                    }
                }

                _generatedForm = formDefinitionElement.ToString();
                _panelXml = formDefinitionElement.Elements().Last().Elements().LastOrDefault();
            }
        }


        /// <exclude />
        public static string GetBindingName(string prefix, string bindingName)
        {
            return string.Format("{0}{1}", prefix, bindingName).Replace('.', '_');
        }


        private string GetBindingName(DataFieldDescriptor dataFieldDescriptor)
        {
            if (string.IsNullOrEmpty(_bindingNamesPrefix))
            {
                return dataFieldDescriptor.Name;
            }

            return GetBindingName(_bindingNamesPrefix, dataFieldDescriptor.Name);
        }



        private string PublicationStatusBindingName
        {
            get
            {
                return GetBindingName(_bindingNamesPrefix, PublicationStatusPostFixBindingName);
            }
        }


        private string PublicationStatusOptionsBindingName
        {
            get
            {
                return GetBindingName(_bindingNamesPrefix, PublicationStatusOptionsPostFixBindingName);
            }
        }

        internal bool BindingIsOptional(string bindingName)
        {
            var customFormDefinition = CustomFormDefinition;

            XElement bindingsXml;

            if (customFormDefinition != null && customFormDefinition.Root != null)
            {
                bindingsXml = customFormDefinition.Root;
            }
            else if (!_generatedForm.IsNullOrEmpty())
            {
                bindingsXml = XElement.Parse(_generatedForm);
            }
            else
            {
                bindingsXml = BindingXml;
            }

            var binding = bindingsXml
                .Descendants(CmsNamespace + "binding")
                .FirstOrDefault(e => (string)e.Attribute("name") == bindingName);

            return binding != null && string.Equals((string)binding.Attribute("optional"), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
