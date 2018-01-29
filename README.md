# Shader for Winform Microsoft VB.Net & C# application

## How to use this library

### Declaration
> Declare the shader as member of the current class.
```
Private WithEvents shader As ShaderScreen = New ShaderScreen()
```

### Setup
> Define the shader property on the constructor.

```
MyBase.Controls.Add(shader)
With shader
	.Size = New Point(MyBase.Width, MyBase.Height)
	.Zone = New Point(MyBase.Location.X, MyBase.Location.Y)
	.Location = New Point(X, Y)
	.Alpha = 30
	.BringToFront()
End With
```
- The Size property define the size of the shader.
- The Zone property define the point that represents the upper-left corner of the control relative to the upper-left corner of its container.
- The Location property define the point that represents the upper-left corner of the control in screen coordinates.
- The Alpha property define the opacity of the shader. This property accept only the values between 0 and 100.

### Recommandation

Since the container and the form may move, the code bellow is recommandated:
```
Sub MyBase_LocationChanged(sender as Object, e as EventArgs) Handles Me.LocationChanged
	Me.shader.Zone = new Point(Me.Location.X, Me.Location.Y)
End Sub
```
When the shader control receive a mouse click, a Clicked signal is emitted. 
You may handle this signal to close the shader with the code bellow:
```
Sub MyShader_Clicked(sender As Object, e As EventArgs) Handles Me.shader.Clicked
	Me.shader.Visible = False
End Sub
```

### Display the shader setup
The Shader Library support the multi-threading, you can use the code bellow:
```
Dim shaderThread as System.Threading.Thread = New System.Threading.Thread(AddressOf Me.shader.picUpdate)
shaderThread.IsBackground = True
shaderThread.Start()
```
Or directly
```
Me.shader.picUpdate()
```