function ConvertToCalData(dev, pnt, val) {
    var calData = new Object();
    calData.EzoDevice = dev;
    if (pnt != null)
        calData.CalibPointName = pnt.val();
    else
        calData.CalibPointName = "";
    calData.Value = val.val();

    console.log(calData);

    return calData;
}