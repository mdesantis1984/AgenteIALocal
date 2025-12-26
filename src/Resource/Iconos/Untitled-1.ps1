$dll="C:\Users\mdesa\.nuget\packages\materialdesignthemes\5.3.0\lib\net462\MaterialDesignThemes.Wpf.dll"
[Reflection.Assembly]::LoadFrom($dll) | Out-Null
[enum]::GetNames([MaterialDesignThemes.Wpf.PackIconKind]) | Where-Object { $_ -match 'Chat' } | Sort-Object


$core="C:\Users\mdesa\.nuget\packages\mahapps.metro.iconpacks.core\6.2.1\lib\net47\MahApps.Metro.IconPacks.Core.dll"
$mat ="C:\Users\mdesa\.nuget\packages\mahapps.metro.iconpacks.material\6.2.1\lib\net47\MahApps.Metro.IconPacks.Material.dll"
[Reflection.Assembly]::LoadFrom($core) | Out-Null
[Reflection.Assembly]::LoadFrom($mat)  | Out-Null
[enum]::GetNames([MahApps.Metro.IconPacks.PackIconMaterialKind]) | Where-Object { $_ -match 'Chat' } | Sort-Object
