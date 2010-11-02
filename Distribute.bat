rmdir Distribute\ /S /Q
mkdir Distribute\
copy Dicom.Dump\bin\Release\*.exe Distribute\ /Y
copy Dicom.Dump\bin\Release\*.dll Distribute\ /Y
copy Dicom.Dump\bin\Release\*.pem Distribute\ /Y
copy Dicom.Dump\bin\Release\*.dic Distribute\ /Y
copy Dicom.Dump\bin\Release\*.txt Distribute\ /Y
copy Dicom.Linq\bin\Release\*.dll Distribute\ /Y
copy Dicom.Scu\bin\Release\*.exe Distribute\ /Y
copy Dicom\bin\Release\Dicom.XML Distribute\ /Y
copy "3rd Party\OpenSSL\*.dll" Distribute\ /Y
del /F /Q Distribute\ssleay32.dll
del /F /Q Distrubite\libeay32.dll
del /F /Q Distribute\*.vshost.exe
