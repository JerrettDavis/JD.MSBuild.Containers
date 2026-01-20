using JD.MSBuild.Fluent;
using JD.MSBuild.Fluent.Fluent;
using JD.MSBuild.Fluent.IR;

namespace JD.MSBuild.Containers.Definition;

public static class DefinitionFactory
{
    public static PackageDefinition Create()
    {
        return Package.Define("JD.MSBuild.Containers")
            .Description("Docker/Container integration for MSBuild projects")
            .Props(ConfigureProps)
            .Targets(ConfigureTargets)
            .Pack(options =>
            {
                options.BuildTransitive = true;
                options.EmitSdk = false;
            })
            .Build();
    }

    private static void ConfigureProps(PropsBuilder props)
    {
        // ========================================
        // Core Enablement
        // ========================================
        
        props.Comment(@"
      Enablement: Disabled by default. Users opt-in by setting DockerEnabled=true.
      
      To enable Docker integration, set:
        <DockerEnabled>true</DockerEnabled>
    ");
        props.Property("DockerEnabled", "false", condition: "'$(DockerEnabled)'==''");

        // ========================================
        // Granular Feature Control
        // Enable/disable specific Docker operations independently
        // ========================================
        
        props.Comment(@"
      Granular Feature Control: Each Docker workflow component can be independently enabled/disabled.
      This provides fine-grained control over what the integration does.
      
      DockerGenerateDockerfile: Controls Dockerfile generation (default: true when DockerEnabled=true)
      DockerBuildImage: Controls Docker image building (default: false, must opt-in)
      DockerRunContainer: Controls Docker container execution (default: false, must opt-in)
      DockerPushImage: Controls pushing to registry (default: false, must opt-in)
      
      Example configurations:
      - Generate-only mode: <DockerGenerateDockerfile>true</DockerGenerateDockerfile> <DockerBuildImage>false</DockerBuildImage>
      - Build-only mode: <DockerGenerateDockerfile>false</DockerGenerateDockerfile> <DockerBuildImage>true</DockerBuildImage>
      - Full automation: <DockerGenerateDockerfile>true</DockerGenerateDockerfile> <DockerBuildImage>true</DockerBuildImage>
    ");
        props.PropertyGroup("'$(DockerGenerateDockerfile)'=='' and '$(DockerEnabled)'=='true'", group =>
        {
            group.Property("DockerGenerateDockerfile", "true");
        });
        props.Property("DockerGenerateDockerfile", "false", condition: "'$(DockerGenerateDockerfile)'==''");
        
        props.Property("DockerBuildImage", "false", condition: "'$(DockerBuildImage)'==''");
        
        props.Property("DockerRunContainer", "false", condition: "'$(DockerRunContainer)'==''");
        
        props.Property("DockerPushImage", "false", condition: "'$(DockerPushImage)'==''");

        // ========================================
        // File and Directory Paths
        // ========================================
        
        props.Comment("Docker output directory for intermediate build artifacts");
        props.Property("DockerOutput", "$(BaseIntermediateOutputPath)docker\\", condition: "'$(DockerOutput)'==''");
        
        props.Comment("Path where generated Dockerfile will be written");
        props.Property("DockerfileOutput", "$(MSBuildProjectDirectory)\\Dockerfile", condition: "'$(DockerfileOutput)'==''");
        
        props.Comment("Path to existing Dockerfile (used in build-only mode)");
        props.Property("DockerfileSource", "$(MSBuildProjectDirectory)\\Dockerfile", condition: "'$(DockerfileSource)'==''");

        // ========================================
        // Docker Image Configuration
        // Image name, tag, and registry settings
        // ========================================
        
        props.Comment("Docker image name (defaults to lowercase AssemblyName)");
        props.Property("DockerImageName", "$(AssemblyName.ToLowerInvariant())", condition: "'$(DockerImageName)'==''");
        
        props.Comment("Docker image tag (e.g., 'latest', '1.0.0', or '$(Version)')");
        props.Property("DockerImageTag", "latest", condition: "'$(DockerImageTag)'==''");
        
        props.Comment("Container registry URL (e.g., 'myregistry.azurecr.io' or 'docker.io/username')");
        props.Property("DockerRegistry", "", condition: "'$(DockerRegistry)'==''");

        // ========================================
        // Base Docker Images
        // Runtime and SDK images for multi-stage builds
        // ========================================
        
        props.Comment("Base runtime image for final stage (e.g., 'mcr.microsoft.com/dotnet/aspnet:8.0')");
        props.Property("DockerBaseImageRuntime", "mcr.microsoft.com/dotnet/aspnet", condition: "'$(DockerBaseImageRuntime)'==''");
        
        props.Comment("Base SDK image for build stages");
        props.Property("DockerBaseImageSdk", "mcr.microsoft.com/dotnet/sdk", condition: "'$(DockerBaseImageSdk)'==''");
        
        props.Comment("Docker image version tag (auto-detected from TargetFramework)");
        props.Property("DockerBaseImageVersion", "$(TargetFrameworkVersion.TrimStart('v'))", condition: "'$(DockerBaseImageVersion)'==''");

        // ========================================
        // Build Context Configuration
        // ========================================
        
        props.Comment("Docker build context directory (usually project root)");
        props.Property("DockerBuildContext", "$(MSBuildProjectDirectory)", condition: "'$(DockerBuildContext)'==''");
        
        props.Comment("Working directory inside the container");
        props.Property("DockerWorkDir", "/app", condition: "'$(DockerWorkDir)'==''");

        // ========================================
        // Container Runtime Configuration
        // ========================================
        
        props.Comment("Container ENTRYPOINT instruction (empty for default)");
        props.Property("DockerEntrypoint", "", condition: "'$(DockerEntrypoint)'==''");
        
        props.Comment("Container CMD instruction (empty for default)");
        props.Property("DockerCmd", "", condition: "'$(DockerCmd)'==''");

        // ========================================
        // Docker Build Options
        // ========================================
        
        props.Comment("Additional Docker build arguments (e.g., '--build-arg VAR=value')");
        props.Property("DockerBuildArgs", "", condition: "'$(DockerBuildArgs)'==''");

        // ========================================
        // Custom Script Integration
        // Execute custom scripts at various lifecycle points
        // ========================================
        
        props.Comment("Path to script executed before Docker build");
        props.Property("DockerPreBuildScript", "", condition: "'$(DockerPreBuildScript)'==''");
        
        props.Comment("Path to script executed after Docker build");
        props.Property("DockerPostBuildScript", "", condition: "'$(DockerPostBuildScript)'==''");
        
        props.Comment("Path to script executed before publish");
        props.Property("DockerPrePublishScript", "", condition: "'$(DockerPrePublishScript)'==''");
        
        props.Comment("Path to script executed after publish");
        props.Property("DockerPostPublishScript", "", condition: "'$(DockerPostPublishScript)'==''");

        // ========================================
        // MSBuild Lifecycle Hook Integration
        // Controls when Docker operations run during build/publish
        // ========================================
        
        props.Comment("DockerGenerateOnBuild: Generate Dockerfile during Build target (true when generation enabled)");
        props.PropertyGroup("'$(DockerGenerateOnBuild)'=='' and '$(DockerGenerateDockerfile)'=='true'", group =>
        {
            group.Property("DockerGenerateOnBuild", "true");
        });
        props.Property("DockerGenerateOnBuild", "false", condition: "'$(DockerGenerateOnBuild)'==''");

        props.Comment("DockerBuildOnBuild: Build Docker image during Build target (false by default, use Publish instead)");
        props.PropertyGroup("'$(DockerBuildOnBuild)'=='' and '$(DockerBuildImage)'=='true'", group =>
        {
            group.Property("DockerBuildOnBuild", "false");
        });
        props.Property("DockerBuildOnBuild", "false", condition: "'$(DockerBuildOnBuild)'==''");

        props.Comment("DockerBuildOnPublish: Build Docker image during Publish target (true when build enabled)");
        props.PropertyGroup("'$(DockerBuildOnPublish)'=='' and '$(DockerBuildImage)'=='true'", group =>
        {
            group.Property("DockerBuildOnPublish", "true");
        });
        props.Property("DockerBuildOnPublish", "false", condition: "'$(DockerBuildOnPublish)'==''");

        props.Comment("DockerRunOnBuild: Run container after Build target (false by default)");
        props.PropertyGroup("'$(DockerRunOnBuild)'=='' and '$(DockerRunContainer)'=='true'", group =>
        {
            group.Property("DockerRunOnBuild", "false");
        });
        props.Property("DockerRunOnBuild", "false", condition: "'$(DockerRunOnBuild)'==''");

        props.Comment("DockerPushOnPublish: Push image to registry after Publish (true when push enabled)");
        props.PropertyGroup("'$(DockerPushOnPublish)'=='' and '$(DockerPushImage)'=='true'", group =>
        {
            group.Property("DockerPushOnPublish", "true");
        });
        props.Property("DockerPushOnPublish", "false", condition: "'$(DockerPushOnPublish)'==''");

        // ========================================
        // Advanced Docker Build Options
        // ========================================
        
        props.Comment("Target platform for multi-arch builds (e.g., 'linux/amd64,linux/arm64')");
        props.Property("DockerBuildPlatform", "", condition: "'$(DockerBuildPlatform)'==''");
        
        props.Comment("Target stage name in multi-stage Dockerfile");
        props.Property("DockerBuildTarget", "", condition: "'$(DockerBuildTarget)'==''");

        // ========================================
        // Docker Run Configuration
        // Port mappings, environment variables, and volumes
        // ========================================
        
        props.Comment("Port mappings for 'docker run' (e.g., '8080:80,8443:443')");
        props.Property("DockerPortMappings", "", condition: "'$(DockerPortMappings)'==''");
        
        props.Comment("Environment variables for container (e.g., 'VAR1=value1;VAR2=value2')");
        props.Property("DockerEnvironmentVariables", "", condition: "'$(DockerEnvironmentVariables)'==''");
        
        props.Comment("Volume mappings for container (e.g., '$(ProjectDir)/data:/app/data')");
        props.Property("DockerVolumeMappings", "", condition: "'$(DockerVolumeMappings)'==''");

        // ========================================
        // Multi-Stage Build Configuration
        // Stage names for generated Dockerfiles
        // ========================================
        
        props.Comment("Use multi-stage builds for optimized images (true for smaller final image)");
        props.Property("DockerUseMultiStage", "true", condition: "'$(DockerUseMultiStage)'==''");
        
        props.Comment("Name of NuGet restore stage in multi-stage build");
        props.Property("DockerRestoreStage", "restore", condition: "'$(DockerRestoreStage)'==''");
        
        props.Comment("Name of build stage in multi-stage build");
        props.Property("DockerBuildStage", "build", condition: "'$(DockerBuildStage)'==''");
        
        props.Comment("Name of publish stage in multi-stage build");
        props.Property("DockerPublishStage", "publish", condition: "'$(DockerPublishStage)'==''");
        
        props.Comment("Name of final runtime stage in multi-stage build");
        props.Property("DockerFinalStage", "final", condition: "'$(DockerFinalStage)'==''");

        // ========================================
        // Project Type Auto-Detection
        // Determines project type for appropriate Dockerfile generation
        // ========================================
        
        props.Comment("Auto-detect project type from OutputType: console for executables");
        props.PropertyGroup("'$(DockerProjectType)'=='' and '$(OutputType)'=='Exe'", group =>
        {
            group.Property("DockerProjectType", "console");
        });
        
        props.Comment("Auto-detect project type from OutputType: library for non-executables");
        props.PropertyGroup("'$(DockerProjectType)'=='' and '$(OutputType)'!='Exe'", group =>
        {
            group.Property("DockerProjectType", "library");
        });

        // ========================================
        // Container Runtime Settings
        // ========================================
        
        props.Comment("Port to EXPOSE in Dockerfile (default 8080 for console apps)");
        props.Property("DockerExposePort", "8080", condition: "'$(DockerExposePort)'=='' and $(DockerProjectType)=='console'");

        // ========================================
        // Incremental Build Support
        // Fingerprinting for change detection
        // ========================================
        
        props.Comment("Path to fingerprint file for incremental build detection");
        props.Property("DockerFingerprintFile", "$(DockerOutput)fingerprint.txt", condition: "'$(DockerFingerprintFile)'==''");
        
        props.Comment("Path to stamp file indicating successful generation");
        props.Property("DockerStampFile", "$(DockerOutput).docker.stamp", condition: "'$(DockerStampFile)'==''");

        // ========================================
        // Diagnostics and Logging
        // ========================================
        
        props.Comment("Logging verbosity: quiet|minimal|normal|detailed|diagnostic");
        props.Property("DockerLogVerbosity", "minimal", condition: "'$(DockerLogVerbosity)'==''");
        
        props.Comment("Dump Docker configuration for debugging");
        props.Property("DockerDumpConfiguration", "false", condition: "'$(DockerDumpConfiguration)'==''");

        // ========================================
        // Docker CLI Configuration
        // ========================================
        
        props.Comment("Docker command (use 'podman' for Podman compatibility)");
        props.Property("DockerCommand", "docker", condition: "'$(DockerCommand)'==''");

        // ========================================
        // NuGet and Build Configuration
        // ========================================
        
        props.Comment("Path to NuGet.config for Docker build context");
        props.Property("DockerNuGetConfigPath", "", condition: "'$(DockerNuGetConfigPath)'==''");
        
        props.Comment("Custom Dockerfile template path (empty for auto-generation)");
        props.Property("DockerTemplateFile", "", condition: "'$(DockerTemplateFile)'==''");

        // ========================================
        // Dockerfile Optimization
        // ========================================
        
        props.Comment("Optimize Docker layers for better caching");
        props.Property("DockerOptimizeLayers", "true", condition: "'$(DockerOptimizeLayers)'==''");
        
        props.Comment("Enable Docker image security scanning");
        props.Property("DockerScanImage", "false", condition: "'$(DockerScanImage)'==''");

        // ========================================
        // Container User Configuration
        // Security: Run container as non-root user
        // ========================================
        
        props.Comment("Username for non-root container user");
        props.Property("DockerUser", "app", condition: "'$(DockerUser)'==''");
        
        props.Comment("Create non-root user in container (recommended for security)");
        props.Property("DockerCreateUser", "true", condition: "'$(DockerCreateUser)'==''");

        // ========================================
        // Script Execution Control
        // Fine-grained control over when scripts run
        // ========================================
        
        props.Comment(@"
      Script Execution Control: Granular control over pre/post script execution.
      Each script type can be independently enabled/disabled.
    ");
        props.PropertyGroup("'$(DockerExecutePreBuildScript)'=='' and '$(DockerPreBuildScript)'!=''", group =>
        {
            group.Property("DockerExecutePreBuildScript", "true");
        });
        props.Property("DockerExecutePreBuildScript", "false", condition: "'$(DockerExecutePreBuildScript)'==''");

        props.Comment("Execute DockerPostBuildScript (auto-enabled when script path is set)");
        props.PropertyGroup("'$(DockerExecutePostBuildScript)'=='' and '$(DockerPostBuildScript)'!=''", group =>
        {
            group.Property("DockerExecutePostBuildScript", "true");
        });
        props.Property("DockerExecutePostBuildScript", "false", condition: "'$(DockerExecutePostBuildScript)'==''");

        props.Comment("Execute DockerPrePublishScript (auto-enabled when script path is set)");
        props.PropertyGroup("'$(DockerExecutePrePublishScript)'=='' and '$(DockerPrePublishScript)'!=''", group =>
        {
            group.Property("DockerExecutePrePublishScript", "true");
        });
        props.Property("DockerExecutePrePublishScript", "false", condition: "'$(DockerExecutePrePublishScript)'==''");

        props.Comment("Execute DockerPostPublishScript (auto-enabled when script path is set)");
        props.PropertyGroup("'$(DockerExecutePostPublishScript)'=='' and '$(DockerPostPublishScript)'!=''", group =>
        {
            group.Property("DockerExecutePostPublishScript", "true");
        });
        props.Property("DockerExecutePostPublishScript", "false", condition: "'$(DockerExecutePostPublishScript)'==''");

        // ========================================
        // Fingerprinting Control
        // ========================================
        
        props.Comment(@"
      Fingerprinting Control: Enable/disable incremental build optimization.
      When disabled, Dockerfile is always regenerated on build.
    ");
        props.Property("DockerUseFingerprinting", "true", condition: "'$(DockerUseFingerprinting)'==''");
    }

    private static void ConfigureTargets(TargetsBuilder targets)
    {
        // Determine the correct task assembly path based on MSBuild runtime and version
        ConfigureTaskAssemblyPath(targets);

        // Register MSBuild tasks
        RegisterTasks(targets);

        // Docker Build Pipeline
        ConfigureDockerPipeline(targets);

        // Clean Integration
        ConfigureCleanTarget(targets);
    }

    private static void ConfigureTaskAssemblyPath(TargetsBuilder targets)
    {
        // Determine the correct task assembly path based on MSBuild runtime and version
        targets.Comment(@"
    Determine the correct task assembly path based on MSBuild runtime and version.
  ");
        targets.PropertyGroup("'$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '18.0'))", group =>
        {
            group.Property("_DockerTasksFolder", "net10.0");
        });
        targets.PropertyGroup("'$(_DockerTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.14'))", group =>
        {
            group.Property("_DockerTasksFolder", "net10.0");
        });
        targets.PropertyGroup("'$(_DockerTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core' and $([MSBuild]::VersionGreaterThanOrEquals('$(MSBuildVersion)', '17.12'))", group =>
        {
            group.Property("_DockerTasksFolder", "net9.0");
        });
        targets.PropertyGroup("'$(_DockerTasksFolder)' == '' and '$(MSBuildRuntimeType)' == 'Core'", group =>
        {
            group.Property("_DockerTasksFolder", "net8.0");
        });

        // Primary path: NuGet package location
        targets.PropertyGroup(null, group =>
        {
            group.Property("_DockerTaskAssembly", "$(MSBuildThisFileDirectory)..\\tasks\\$(_DockerTasksFolder)\\JD.MSBuild.Containers.Tasks.dll");
        });

        // Fallback path: Local development (when building from source)
        targets.PropertyGroup("!Exists('$(_DockerTaskAssembly)')", group =>
        {
            group.Property("_DockerTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.MSBuild.Containers.Tasks\\bin\\$(Configuration)\\$(_DockerTasksFolder)\\JD.MSBuild.Containers.Tasks.dll");
        });
        targets.PropertyGroup("!Exists('$(_DockerTaskAssembly)') and '$(Configuration)' == ''", group =>
        {
            group.Property("_DockerTaskAssembly", "$(MSBuildThisFileDirectory)..\\..\\JD.MSBuild.Containers.Tasks\\bin\\Debug\\$(_DockerTasksFolder)\\JD.MSBuild.Containers.Tasks.dll");
        });
    }

    private static void RegisterTasks(TargetsBuilder targets)
    {
        targets.Comment(@"
    Register MSBuild tasks.
  ");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.ResolveDockerInputs", "$(_DockerTaskAssembly)");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.GenerateDockerfile", "$(_DockerTaskAssembly)");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.ComputeDockerFingerprint", "$(_DockerTaskAssembly)");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.ExecuteDockerBuild", "$(_DockerTaskAssembly)");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.ExecuteDockerRun", "$(_DockerTaskAssembly)");
        targets.UsingTask("JD.MSBuild.Containers.Tasks.ExecuteDockerScript", "$(_DockerTaskAssembly)");
    }

    private static void ConfigureDockerPipeline(TargetsBuilder targets)
    {
        // Docker Build Pipeline section header
        targets.Comment(@"
    ========================================================================
    Docker Build Pipeline: Generate Dockerfile and build container image
    ========================================================================
    
    This pipeline supports multiple configuration modes:
    1. Generate-only: DockerGenerateDockerfile=true, DockerBuildImage=false
    2. Build-only: DockerGenerateDockerfile=false, DockerBuildImage=true (uses existing Dockerfile)
    3. Full automation: DockerGenerateDockerfile=true, DockerBuildImage=true
    4. Custom hooks: Enable specific pre/post scripts independently
  ");

        // Lifecycle hook: BeforeDockerGeneration
        targets.Target("BeforeDockerGeneration", target =>
        {
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerGenerateDockerfile)' == 'true'");
        });

        // Resolve Docker inputs and project information
        targets.Target("DockerResolveInputs", target =>
        {
            target.DependsOnTargets("BeforeDockerGeneration");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerGenerateDockerfile)' == 'true'");

            target.Task("ResolveDockerInputs", task =>
            {
                task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                task.Param("AssemblyName", "$(AssemblyName)");
                task.Param("TargetFramework", "$(TargetFramework)");
                task.Param("OutputType", "$(OutputType)");
                task.Param("RuntimeIdentifier", "$(RuntimeIdentifier)");
                task.Param("OutputPath", "$(OutputPath)");
                task.Param("PackageReferences", "@(PackageReference)");
                task.Param("IsWebApplication", "$([System.Convert]::ToBoolean('$(UsingMicrosoftNETSdkWeb)'))");
                task.OutputProperty("ProjectType", "_DockerResolvedProjectType");
                task.OutputProperty("BaseImage", "_DockerResolvedBaseImageRuntime");
                task.OutputProperty("SdkImage", "_DockerResolvedBaseImageSdk");
                task.OutputProperty("EntryPoint", "_DockerResolvedEntrypoint");
                task.OutputProperty("WorkingDirectory", "_DockerResolvedWorkDir");
                task.OutputProperty("ExposedPorts", "_DockerResolvedExposePort");
            });
        });

        // Compute fingerprint for incremental builds
        targets.Target("DockerComputeFingerprint", target =>
        {
            target.DependsOnTargets("DockerResolveInputs");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerGenerateDockerfile)' == 'true' and '$(DockerUseFingerprinting)' == 'true'");

            target.Task("MakeDir", task => task.Param("Directories", "$(DockerOutput)"));

            target.Task("ComputeDockerFingerprint", task =>
            {
                task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                task.Param("ProjectDirectory", "$(MSBuildProjectDirectory)");
                task.Param("DockerfilePath", "$(DockerfileOutput)");
                task.Param("TargetFramework", "$(TargetFramework)");
                task.Param("Configuration", "$(Configuration)");
                task.Param("BaseImage", "$(_DockerResolvedBaseImageRuntime)");
                task.Param("SdkImage", "$(_DockerResolvedBaseImageSdk)");
                task.Param("PackageReferences", "@(PackageReference)");
                task.Param("EnvironmentVariables", "$(DockerEnvironmentVariables)");
                task.Param("FingerprintFile", "$(DockerFingerprintFile)");
                task.Param("IncludeGeneratedFiles", "false");
                task.OutputProperty("Fingerprint", "_DockerFingerprint");
                task.OutputProperty("HasChanged", "_DockerFingerprintChanged");
            });
        });

        // Generate Dockerfile
        targets.Target("DockerGenerateDockerfile", target =>
        {
            target.DependsOnTargets("DockerComputeFingerprint");
            target.BeforeTargets("CoreCompile");
            target.Inputs("$(MSBuildProjectFullPath)");
            target.Outputs("$(DockerStampFile)");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerGenerateDockerfile)' == 'true' and '$(DockerGenerateOnBuild)' == 'true' and ('$(DockerUseFingerprinting)' != 'true' or '$(_DockerFingerprintChanged)' == 'true' or !Exists('$(DockerStampFile)'))");

            target.Task("GenerateDockerfile", task =>
            {
                task.Param("ProjectPath", "$(MSBuildProjectFullPath)");
                task.Param("ProjectType", "$(_DockerResolvedProjectType)");
                task.Param("BaseImage", "$(_DockerResolvedBaseImageRuntime)");
                task.Param("SdkImage", "$(_DockerResolvedBaseImageSdk)");
                task.Param("AssemblyName", "$(AssemblyName)");
                task.Param("WorkingDirectory", "$(DockerWorkDir)");
                task.Param("ExposedPorts", "$(_DockerResolvedExposePort)");
                task.Param("EnvironmentVariables", "$(DockerEnvironmentVariables)");
                task.Param("OutputPath", "$(DockerfileOutput)");
                task.Param("TargetFramework", "$(TargetFramework)");
                task.Param("GenerateDockerIgnore", "true");
            });

            target.Task("WriteLinesToFile", task =>
            {
                task.Param("File", "$(DockerStampFile)");
                task.Param("Lines", "$(_DockerFingerprint)");
                task.Param("Overwrite", "true");
            }, condition: "'$(DockerUseFingerprinting)' == 'true'");

            target.Message("Generated Dockerfile: $(DockerfileOutput)", importance: "High");
        });

        // Lifecycle hook: AfterDockerGeneration
        targets.Target("AfterDockerGeneration", target =>
        {
            target.AfterTargets("DockerGenerateDockerfile");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerGenerateDockerfile)' == 'true'");
        });

        // Docker build integration
        ConfigureDockerBuildTargets(targets);

        // Docker run integration
        ConfigureDockerRunTargets(targets);

        // Publish integration
        ConfigureDockerPublishTargets(targets);
    }

    private static void ConfigureDockerBuildTargets(TargetsBuilder targets)
    {
        // Docker Build Integration section header
        targets.Comment(@"
    ========================================================================
    Docker Build Integration with MSBuild Hooks
    ========================================================================
    These targets respect the granular configuration options:
    - DockerExecutePreBuildScript: Controls pre-build script execution
    - DockerBuildOnBuild: Controls whether Docker build runs during MSBuild Build
    - DockerExecutePostBuildScript: Controls post-build script execution
  ");

        // Pre-build script execution
        targets.Target("DockerExecutePreBuildScript", target =>
        {
            target.BeforeTargets("DockerBuild");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerExecutePreBuildScript)' == 'true' and '$(DockerPreBuildScript)' != '' and Exists('$(DockerPreBuildScript)')");

            target.Task("ExecuteDockerScript", task =>
            {
                task.Param("ScriptPath", "$(DockerPreBuildScript)");
                task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
            });

            target.Message("Executed pre-build script: $(DockerPreBuildScript)", importance: "High");
        });

        // Lifecycle hook: BeforeDockerBuild
        targets.Target("BeforeDockerBuild", target =>
        {
            target.DependsOnTargets("DockerExecutePreBuildScript");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");
        });

        // Validate Dockerfile exists for build-only mode
        targets.Target("DockerValidateExistingDockerfile", target =>
        {
            target.BeforeTargets("DockerBuild");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerGenerateDockerfile)' != 'true'");

            target.Comment(@"
    Validate Dockerfile exists for build-only mode.
    When DockerGenerateDockerfile=false and DockerBuildImage=true, we need an existing Dockerfile.
  ");
            target.PropertyGroup("'$(DockerfileSource)' != ''", group =>
            {
                group.Property("_DockerfileToUse", "$(DockerfileSource)");
            });
            target.PropertyGroup("'$(_DockerfileToUse)' == ''", group =>
            {
                group.Property("_DockerfileToUse", "$(DockerfileOutput)");
            });

            target.Task("Error", task =>
            {
                task.Param("Text", "Docker build is enabled but no Dockerfile was found at: $(_DockerfileToUse). Either enable DockerGenerateDockerfile=true or provide an existing Dockerfile at DockerfileSource.");
            }, condition: "!Exists('$(_DockerfileToUse)')");

            target.Message("Using existing Dockerfile for build: $(_DockerfileToUse)", importance: "High");
        });

        // Docker build execution
        targets.Target("DockerBuild", target =>
        {
            target.DependsOnTargets("BeforeDockerBuild;DockerValidateExistingDockerfile;Build");
            target.AfterTargets("Build");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");

            target.PropertyGroup("'$(DockerGenerateDockerfile)' == 'true'", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileOutput)");
            });
            target.PropertyGroup("'$(_DockerfilePath)' == '' and '$(DockerfileSource)' != ''", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileSource)");
            });
            target.PropertyGroup("'$(_DockerfilePath)' == ''", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileOutput)");
            });
            
            // Compute full image tag (registry/name:tag)
            target.PropertyGroup("'$(DockerRegistry)' != ''", group =>
            {
                group.Property("_DockerImageTag", "$(DockerRegistry)/$(DockerImageName):$(DockerImageTag)");
            });
            target.PropertyGroup("'$(_DockerImageTag)' == ''", group =>
            {
                group.Property("_DockerImageTag", "$(DockerImageName):$(DockerImageTag)");
            });

            target.Task("ExecuteDockerBuild", task =>
            {
                task.Param("DockerfilePath", "$(_DockerfilePath)");
                task.Param("BuildContext", "$(DockerBuildContext)");
                task.Param("ImageTag", "$(_DockerImageTag)");
                task.Param("BuildArgs", "$(DockerBuildArgs)");
                task.Param("Platform", "$(DockerBuildPlatform)");
                task.Param("Target", "$(DockerBuildTarget)");
            });

            target.Message("Docker image built: $(_DockerImageTag)", importance: "High");
        });

        // Post-build script execution
        targets.Target("DockerExecutePostBuildScript", target =>
        {
            target.AfterTargets("DockerBuild");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerExecutePostBuildScript)' == 'true' and '$(DockerPostBuildScript)' != '' and Exists('$(DockerPostBuildScript)')");

            target.Task("ExecuteDockerScript", task =>
            {
                task.Param("ScriptPath", "$(DockerPostBuildScript)");
                task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
            });

            target.Message("Executed post-build script: $(DockerPostBuildScript)", importance: "High");
        });

        // Lifecycle hook: AfterDockerBuild
        targets.Target("AfterDockerBuild", target =>
        {
            target.AfterTargets("DockerExecutePostBuildScript");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");
        });
    }

    private static void ConfigureDockerRunTargets(TargetsBuilder targets)
    {
        // Docker Run Integration section header
        targets.Comment(@"
    ========================================================================
    Docker Run Integration
    ========================================================================
    Runs container after build (opt-in via DockerRunContainer=true and DockerRunOnBuild=true)
  ");

        // Lifecycle hook: BeforeDockerRun
        targets.Target("BeforeDockerRun", target =>
        {
            target.DependsOnTargets("DockerBuild");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerRunContainer)' == 'true' and '$(DockerRunOnBuild)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");
        });

        // Docker run execution
        targets.Target("DockerRun", target =>
        {
            target.DependsOnTargets("BeforeDockerRun");
            target.AfterTargets("DockerBuild");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerRunContainer)' == 'true' and '$(DockerRunOnBuild)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");

            target.Task("ExecuteDockerRun", task =>
            {
                task.Param("ImageTag", "$(_DockerImageTag)");
                task.Param("PortMappings", "$(DockerPortMappings)");
                task.Param("EnvironmentVariables", "$(DockerEnvironmentVariables)");
                task.Param("VolumeMounts", "$(DockerVolumeMappings)");
            });

            target.Message("Docker container started successfully", importance: "High");
        });

        // Lifecycle hook: AfterDockerRun
        targets.Target("AfterDockerRun", target =>
        {
            target.AfterTargets("DockerRun");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerRunContainer)' == 'true' and '$(DockerRunOnBuild)' == 'true' and '$(DockerBuildOnBuild)' == 'true'");
        });
    }

    private static void ConfigureDockerPublishTargets(TargetsBuilder targets)
    {
        // Publish Integration section header
        targets.Comment(@"
    ========================================================================
    Publish Integration
    ========================================================================
    These targets run during Publish and respect DockerBuildOnPublish setting.
  ");

        // Pre-publish script execution
        targets.Target("DockerExecutePrePublishScript", target =>
        {
            target.BeforeTargets("DockerPublish");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerExecutePrePublishScript)' == 'true' and '$(DockerPrePublishScript)' != '' and Exists('$(DockerPrePublishScript)')");

            target.Task("ExecuteDockerScript", task =>
            {
                task.Param("ScriptPath", "$(DockerPrePublishScript)");
                task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
            });

            target.Message("Executed pre-publish script: $(DockerPrePublishScript)", importance: "High");
        });

        // Validate Dockerfile exists for publish
        targets.Target("DockerValidateDockerfileForPublish", target =>
        {
            target.BeforeTargets("DockerPublish");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerBuildOnPublish)' == 'true' and '$(DockerGenerateDockerfile)' != 'true'");

            target.PropertyGroup("'$(DockerfileSource)' != ''", group =>
            {
                group.Property("_DockerfileToUse", "$(DockerfileSource)");
            });
            target.PropertyGroup("'$(_DockerfileToUse)' == ''", group =>
            {
                group.Property("_DockerfileToUse", "$(DockerfileOutput)");
            });

            target.Task("Error", task =>
            {
                task.Param("Text", "Docker build on publish is enabled but no Dockerfile was found at: $(_DockerfileToUse). Either enable DockerGenerateDockerfile=true or provide an existing Dockerfile at DockerfileSource.");
            }, condition: "!Exists('$(_DockerfileToUse)')");
        });

        // Docker build on publish
        targets.Target("DockerPublish", target =>
        {
            target.DependsOnTargets("DockerExecutePrePublishScript;DockerValidateDockerfileForPublish;Publish");
            target.AfterTargets("Publish");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerBuildImage)' == 'true' and '$(DockerBuildOnPublish)' == 'true'");

            target.PropertyGroup("'$(DockerGenerateDockerfile)' == 'true'", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileOutput)");
            });
            target.PropertyGroup("'$(_DockerfilePath)' == '' and '$(DockerfileSource)' != ''", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileSource)");
            });
            target.PropertyGroup("'$(_DockerfilePath)' == ''", group =>
            {
                group.Property("_DockerfilePath", "$(DockerfileOutput)");
            });
            
            // Compute full image tag (registry/name:tag)
            target.PropertyGroup("'$(DockerRegistry)' != ''", group =>
            {
                group.Property("_DockerImageTag", "$(DockerRegistry)/$(DockerImageName):$(DockerImageTag)");
            });
            target.PropertyGroup("'$(_DockerImageTag)' == ''", group =>
            {
                group.Property("_DockerImageTag", "$(DockerImageName):$(DockerImageTag)");
            });

            target.Task("ExecuteDockerBuild", task =>
            {
                task.Param("DockerfilePath", "$(_DockerfilePath)");
                task.Param("BuildContext", "$(DockerBuildContext)");
                task.Param("ImageTag", "$(_DockerImageTag)");
                task.Param("BuildArgs", "$(DockerBuildArgs)");
                task.Param("Platform", "$(DockerBuildPlatform)");
                task.Param("Target", "$(DockerBuildTarget)");
            });

            target.Message("Docker image published: $(_DockerImageTag)", importance: "High");
        });

        // Post-publish script execution
        targets.Target("DockerExecutePostPublishScript", target =>
        {
            target.AfterTargets("DockerPublish");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerExecutePostPublishScript)' == 'true' and '$(DockerPostPublishScript)' != '' and Exists('$(DockerPostPublishScript)')");

            target.Task("ExecuteDockerScript", task =>
            {
                task.Param("ScriptPath", "$(DockerPostPublishScript)");
                task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
            });

            target.Message("Executed post-publish script: $(DockerPostPublishScript)", importance: "High");
        });

        // Push to registry
        targets.Target("DockerPushImage", target =>
        {
            target.AfterTargets("DockerPublish");
            target.Condition("'$(DockerEnabled)' == 'true' and '$(DockerPushImage)' == 'true' and '$(DockerPushOnPublish)' == 'true' and '$(DockerRegistry)' != ''");

            target.PropertyGroup("'$(DockerRegistry)' != ''", group =>
            {
                group.Property("_DockerFullImageName", "$(DockerRegistry)/$(DockerImageName):$(DockerImageTag)");
            });
            target.PropertyGroup("'$(DockerRegistry)' == ''", group =>
            {
                group.Property("_DockerFullImageName", "$(DockerImageName):$(DockerImageTag)");
            });

            target.Task("Exec", task =>
            {
                task.Param("Command", "$(DockerCommand) push $(_DockerFullImageName)");
                task.Param("WorkingDirectory", "$(MSBuildProjectDirectory)");
                task.Param("ConsoleToMSBuild", "true");
                task.OutputProperty("ConsoleOutput", "_DockerPushOutput");
            });

            target.Message("Docker image pushed to registry: $(_DockerFullImageName)", importance: "High");
        });
    }

    private static void ConfigureCleanTarget(TargetsBuilder targets)
    {
        // Clean Integration section header
        targets.Comment(@"
    ========================================================================
    Clean Integration
    ========================================================================
  ");

        targets.Target("DockerClean", target =>
        {
            target.AfterTargets("Clean");
            target.Condition("'$(DockerEnabled)' == 'true'");

            target.Message("Cleaning Docker output: $(DockerOutput)", importance: "Normal");

            target.Task("RemoveDir", task =>
            {
                task.Param("Directories", "$(DockerOutput)");
            }, condition: "Exists('$(DockerOutput)')");

            target.Task("Delete", task =>
            {
                task.Param("Files", "$(DockerfileOutput)");
            }, condition: "'$(DockerGenerateDockerfile)' == 'true' and Exists('$(DockerfileOutput)')");

            target.Message("Cleaned generated Dockerfile: $(DockerfileOutput)", 
                importance: "Normal", 
                condition: "'$(DockerGenerateDockerfile)' == 'true' and Exists('$(DockerfileOutput)')");
        });
    }
}
