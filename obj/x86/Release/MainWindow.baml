   M S B A M L   `   `   `  ����   CoordinateMappingBasics-WPFA    ;Microsoft.Samples.Kinect.CoordinateMappingBasics.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  Kinect homework��$	��600��$	��700�� � 	Resources    MediumGreyBrush �� Color  KinectPurpleBrush  KinectBlueBrush �� 
TargetType �� Property �� Value  ScreenshotButton �� 
TargetType �� Orientation ��Name	 �� TextAlignment
 ��Name �� Triggers P� Property P� Value �� 
TargetName��%�  &	        &	      &	 *     '�� ?     &	 t     7  7  �� $ 	#ff6e6e6e���� $ 	#ff52318f���� $ 	#ff00BCF2���� " ��.���  �@J�SnapsToDevicePixels . �� " ��.���  � �� �� " ��.R�� .+��� $ 
Horizontal=�
������ .+��� $ ScreenshotText��#��z��$
	 Left=�$��Center=�8��  $��15(�� $��9,0,0,0q�.+��� $
 ScreenshotNormal��$!��Images\ScreenshotNormal.png��$
��None=�$��Center=��� $
 ScreenshotHover��$ ��Images\ScreenshotHover.png��$
��None=�$��Center=�$y�	Collapsed=� P�  �@J�IsMouseOver . . ��� $ ScreenshotNormal�� � y�$ 	Collapsed=��� $ ScreenshotHover�� � y�$ Visible=��� 8  $ ScreenshotText�� � ��.��� $��	10 0 10 0q� � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto �.+��� $��0��$��Images\Status.png��$
��None=�$��Center=�$��0 0 0 5q�(� $��1��$��Center=�.��� -   $��CompositeImage��.+��� -   $
 BackgroundImage��$��Images\Background.png��$��UniformToFill=�$��4���� $��UniformToFill=�$��
0,0,-163,0q����� *ImageSource+�� -   $
 Foreground_eggcake��$��10,492,797,10q�$��Images/eggcake.jpg��$y�Hidden=��� -   $
 Foreground_torii��$��Images/torii.png��$��380,238,-219,55q�$y�Hidden=��� -   $
 Foreground_heli��$��Images/heli.png��$��-18,517,513,10q�$y�Hidden=��� -   $
 Item_photographer��$"��Images/item_photographer.png��$��216,1149,973,425q�$y�Hidden=��� -   $
 Item_bag��$��Images/item_bag.png��$��1087,3070,1038,676q�$y�Hidden=� J� RenderTransformOrigin$ 0.5,0.5&��� -	   $��2��#��[ $��
Screenshot��$
��Auto��$��Right=�$��Center=�$��
10 10 0 10q��� -
   $��	statusBar��$��3��$��Stretch=�$��Bottom=�
�������#��[  .@��� ���� *
StatusText+�� -    ��Name$ ComboCountry��$��20��$��70��$��32,20,571.6,15.2q��� -   $ 	ComboItem��$��20��$��70��$��150,20,453.6,15.2q�$ 0.348,0.472&��� -   $ ComboForeground��$��20��$��70��$��431,20,172.6,15.2q��� -   $ 
ComboCloth��$��20��$��70��$��554,20,49.6,15.2q�