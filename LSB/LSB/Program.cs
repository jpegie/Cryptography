using LSB.Classes;
using LSB.Classes.DataProviders;

var imageProvider = new LocalFileProvider("C:\\Users\\Владимир\\Downloads\\мем1.jpg");
var txtProvider = new LocalFileProvider("C:\\Users\\Владимир\\Downloads\\text.txt");
imageProvider.LoadData();
txtProvider.LoadData();

var key = KeyCreator.CreateKey(imageProvider, txtProvider);
var lsb = new LSB.Classes.LSB(imageProvider, txtProvider, key);

var image = lsb.HideData();

File.WriteAllBytes("C:\\Users\\Владимир\\Downloads\\мем1_.jpg", image.ToArray());


