Imports System
Imports System.ComponentModel
Imports System.Web.UI

Imports DevExpress.ExpressApp.Web.Editors.ASPx
Imports DevExpress.Web
Imports DevExpress.ExpressApp.Editors
Imports DevExpress.ExpressApp.DC
Imports DevExpress.ExpressApp.Model
Imports System.Web.UI.WebControls
Imports DevExpress.ExpressApp.Web.Editors

Namespace WebExample.Module.Web
    <ListEditor(GetType(Object), False)> _
    Public Class MultiEditASPxGridListEditor
        Inherits ASPxGridListEditor
        Public Sub New(ByVal model As IModelListView)
            MyBase.New(model)
            AddHandler CreateCustomDataItemTemplate, AddressOf OnCreateCustomDataItemTemplate
            AddHandler CustomizeGridViewDataColumn, AddressOf OnCustomizeGridViewDataColumn
        End Sub
        Private callback As ASPxCallback
        Protected Overrides Function CreateControlsCore() As Object
            Dim panel As New Panel()
            callback = New ASPxCallback()
            callback.ID = Model.Id & "aspxCallback1"
            callback.ClientInstanceName = Model.Id & "_callback1"
            AddHandler callback.Callback, AddressOf callback_Callback
            panel.Controls.Add(callback)
            Dim grid As ASPxGridView = CType(MyBase.CreateControlsCore(), ASPxGridView)
            panel.Controls.Add(grid)
            Return panel
        End Function
        Private Sub OnCreateCustomDataItemTemplate(ByVal sender As Object, ByVal e As CreateCustomDataItemTemplateEventArgs)
            if IsColumnSupported(e.ModelColumn) Then
                Dim propertyEditor As WebPropertyEditor = FindPropertyEditor(e.ModelColumn, ViewEditMode.Edit)
                If propertyEditor Is Nothing Then
                    propertyEditor = DataItemTemplateFactory.CreateColumnTemplate(e.ModelColumn, e.DataItemTemplateInfoProvider, Application, ObjectTypeInfo, ObjectSpace, ViewEditMode.Edit).PropertyEditor
                End If
                propertyEditor.ImmediatePostData = false
                e.Template = New MultiRowEditTemplate(propertyEditor, callback.ClientInstanceName)
                e.Handled = True
            End If
        End Sub
        Private Sub callback_Callback(ByVal source As Object, ByVal e As DevExpress.Web.CallbackEventArgs)
            Dim p() As String = e.Parameter.Split("|"c)
            Dim key As Object = TypeDescriptor.GetConverter(ObjectTypeInfo.KeyMember.MemberType).ConvertFromString(p(0))
            Dim member As IMemberInfo = ObjectTypeInfo.FindMember(p(1))
            Dim value As Object = TypeDescriptor.GetConverter(member.MemberType).ConvertFromString(p(2))

            Dim obj As Object = ObjectSpace.GetObjectByKey(ObjectTypeInfo.Type, key)
            member.SetValue(obj, value)
            ObjectSpace.CommitChanges()
        End Sub
        Private supportedPropertyEditorTypes() As Type = { GetType(ASPxStringPropertyEditor), GetType(ASPxIntPropertyEditor), GetType(ASPxBooleanPropertyEditor), GetType(ASPxEnumPropertyEditor) }
        Protected Overridable Function IsColumnSupported(ByVal model As IModelColumn) As Boolean
            If model.GroupIndex >= 0 Then
                Return False
            End If
            For Each type As Type In supportedPropertyEditorTypes
                If type.IsAssignableFrom(model.PropertyEditorType) Then
                    Return True
                End If
            Next type
            Return False
        End Function
        ' Sorting and grouping are not supported
        Private Sub OnCustomizeGridViewDataColumn(ByVal sender As Object, ByVal e As CustomizeGridViewDataColumnEventArgs)
            If IsColumnSupported(e.ModelColumn) Then
                e.Column.Settings.AllowSort = DevExpress.Utils.DefaultBoolean.False
                e.Column.Settings.AllowGroup = DevExpress.Utils.DefaultBoolean.False
            End If
        End Sub
    End Class
    Public Class MultiRowEditTemplate
        Implements IBehaviourTemplate
        Private propertyEditor As WebPropertyEditor
        Private callbackName As String
        Private Const CallbackArgumentFormat As String = "function (s, e) {{ {0}.PerformCallback(""{1}|{2}|"" + {3}); }}" ' ASPxCallback, key, fieldName, value

        Public Sub New(ByVal propertyEditor As WebPropertyEditor, ByVal callbackName As String)
            Me.propertyEditor = propertyEditor
            Me.callbackName = callbackName
        End Sub
        #Region "IBehaviourTemplate Members"

        Public Sub InstantiateIn(ByVal container As Control) Implements ITemplate.InstantiateIn
            Dim gridContainer As GridViewDataItemTemplateContainer = TryCast(container, GridViewDataItemTemplateContainer)
            If gridContainer Is Nothing Then
                Throw New NotSupportedException(container.GetType().FullName)
            End If
            propertyEditor.CreateControl()
            Dim obj As Object = gridContainer.Grid.GetRow(gridContainer.VisibleIndex)
            propertyEditor.CurrentObject = obj
            propertyEditor.ReadValue()
            If TypeOf propertyEditor.Editor Is ASPxWebControl Then
                CType(propertyEditor.Editor, ASPxWebControl).SetClientSideEventHandler("ValueChanged", String.Format(CallbackArgumentFormat, callbackName, gridContainer.KeyValue, gridContainer.Column.FieldName, "s.GetValue()"))
            End If
            container.Controls.Add(CType(propertyEditor.Control, Control))
        End Sub

        Public ReadOnly Property CancelClickEventPropagation() As Boolean Implements IBehaviourTemplate.CancelClickEventPropagation
            Get
                Return True
            End Get
        End Property

        #End Region
    End Class
End Namespace