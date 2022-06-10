param(
    $Flag
)

$exeproj="docfxkicker"
$templatedir="template"
$exeoutdir=$exeproj + "/bin/Release"

$pkgproj="Packages"
$packtmp=$pkgproj + "/tmp"
$packworkdir=$pkgproj + "/obj"
$packoutdir=$pkgproj + "/bin"

if(Test-Path $exeoutdir ){
    rm -Recurse -Force $exeoutdir
}
if(Test-Path $pkgproj ){
    rm -Recurse -Force $pkgproj
}

if( -not ( Test-Path $pkgproj ) ){
    mkdir $pkgproj
}
mkdir $packworkdir
mkdir ${packworkdir}/build

copy -Recurse $templatedir $packworkdir/

dotnet build $exeproj -c Release
dotnet pack  $exeproj -c Release

# get nuspec from nupkg
$nupkglist=ls $exeoutdir/*.nupkg
mv $nupkglist[0] $pkgproj/pack.zip
Expand-Archive $pkgproj/pack.zip -DestinationPath $pkgproj/pack

mv  "$pkgproj/pack/${exeproj}.nuspec" ${packworkdir}
copy ${exeproj}/bin/Release/net472/*  ${packworkdir}/build -Recurse
copy ${exeproj}/${exeproj}.targets    ${packworkdir}/build


nuget pack ${packworkdir}/${exeproj}.nuspec -NoPackageAnalysis -NonInteractive -OutputDir $packoutdir