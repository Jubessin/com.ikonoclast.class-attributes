ClassAttributes

Unity package used in the development of Ikonoclast projects, containing a set of attributes that can be configured, applied to classes, and enforced when the project loads or the hierarchy changes.

## Editor Tooling

- ClassAttributeEnforcerEditorWindow
- .configurations.json

## Runtime Support

- RequireNameAttribute
- RequireLayerAttribute
- DisallowComponentAttribute
- RequireComponentAttribute
- RequireChildComponentAttribute

## Dependencies

This package has dependencies. To use this package, add the following to the manifest.json file found under Assets > Packages:

* `"com.ikonoclast.common" : "https://github.com/Jubessin/com.ikonoclast.common.git"`
