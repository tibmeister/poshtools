try{

 Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase,system.windows.forms

} catch {

 Throw "Failed to load Windows Presentation Framework assemblies."

}

[xml]$Global:xmlWPF = Get-Content -Path .\$itemname$
$Global:xamGUI = [Windows.Markup.XamlReader]::Load((new-object System.Xml.XmlNodeReader $xmlWPF))
$xamGUI.ShowDialog()
