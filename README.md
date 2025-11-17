# Requirements
- `Library\PackageCache\com.unity.render-pipelines.core@XXXXXXXXXXXX\Runtime\GPUDriven\InstanceCullingBatcherBurst.cs`
  - ```cs
    // line 86, replace:
    return ref data;
    // with:
    return ref drawRanges.ElementAt(drawRangeIndex);
  - ```cs
    // line 110, replace:
    return ref data;
    // with:
    return ref drawBatches.ElementAt(drawBatchIndex);`
- `Library\PackageCache\com.unity.render-pipelines.core@XXXXXXXXXXXX\Runtime\RenderGraph\Compiler\NativePassCompiler.cs`
  - ```cs
    // line 1285, replace:
    currLoadAudit = ref nativePass.loadAudit[nativePass.loadAudit.size - 1];
    // with:
    currLoadAudit = nativePass.loadAudit[nativePass.loadAudit.size - 1];
  - ```cs
    // line 1288, replace:
    currStoreAudit = ref nativePass.storeAudit[nativePass.storeAudit.size - 1];
    // with:
    currStoreAudit = nativePass.storeAudit[nativePass.storeAudit.size - 1];
- `Library\PackageCache\com.unity.render-pipelines.universal@XXXXXXXXXXXX\Editor\2D\Converter\Base2DMaterialUpgrader.cs`
  - ```cs
    // line 9, remove:
    using System.Runtime.Remoting.Messaging;
