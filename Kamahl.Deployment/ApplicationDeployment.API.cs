#undef DelegatesImplemented
#undef EventsImplemented

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kamahl.Deployment
{
    internal partial interface IApplicationDeployment
    {
        // Summary:
        //     Gets the URL used to launch the deployment manifest of the application.
        //
        // Returns:
        //     A zero-length string if the TrustUrlParameters property in the deployment
        //     manifest is false, or if the user has supplied a UNC to open the deployment
        //     or has opened it locally. Otherwise, the return value is the full URL used
        //     to launch the application, including any parameters.
        Uri ActivationUri { get; }

        //
        // Summary:
        //     Gets the version of the deployment for the current running instance of the
        //     application.
        //
        // Returns:
        //     The current deployment version.
        Version CurrentVersion { get; }
        //
        // Summary:
        //     Gets the path to the ClickOnce data directory.
        //
        // Returns:
        //     A string containing the path to the application's data directory on the local
        //     disk.
        string DataDirectory { get; }
        //
        // Summary:
        //     Gets a value indicating whether this is the first time this application has
        //     run on the client computer.
        //
        // Returns:
        //     true if this version of the application has never run on the client computer
        //     before; otherwise, false.
        bool IsFirstRun { get; }
        //
        // Summary:
        //     Gets the date and the time ClickOnce last checked for an application update.
        //
        // Returns:
        //     The System.DateTime of the last update check.
        DateTime TimeOfLastUpdateCheck { get; }
        //
        // Summary:
        //     Gets the full name of the application after it has been updated.
        //
        // Returns:
        //     A System.String that contains the full name of the application.
        string UpdatedApplicationFullName { get; }
        //
        // Summary:
        //     Gets the version of the update that was recently downloaded.
        //
        // Returns:
        //     The System.Version describing the version of the update.
        Version UpdatedVersion { get; }
        //
        // Summary:
        //     Gets the Web site or file share from which this application updates itself.
        //
        // Returns:
        //     The update path, expressed as an HTTP, HTTPS, or file URL; or as a Windows
        //     network file path (UNC).
        Uri UpdateLocation { get; }


#if EventsImplemented
        // Summary:
        //     Occurs when System.Deployment.Application.ApplicationDeployment.CheckForUpdateAsync()
        //     has completed.
        public event CheckForUpdateCompletedEventHandler CheckForUpdateCompleted;
        //
        // Summary:
        //     Occurs when a progress update is available on a System.Deployment.Application.ApplicationDeployment.CheckForUpdateAsync()
        //     call.
        public event DeploymentProgressChangedEventHandler CheckForUpdateProgressChanged;
        //
        // Summary:
        //     Occurs on the main application thread when a file download is complete.
        public event DownloadFileGroupCompletedEventHandler DownloadFileGroupCompleted;
        //
        // Summary:
        //     Occurs when status information is available on a file download operation
        //     initiated by a call to Overload:System.Deployment.Application.ApplicationDeployment.DownloadFileGroupAsync.
        public event DeploymentProgressChangedEventHandler DownloadFileGroupProgressChanged;
        //
        // Summary:
        //     Occurs when ClickOnce has finished upgrading the application as the result
        //     of a call to System.Deployment.Application.ApplicationDeployment.UpdateAsync().
        public event AsyncCompletedEventHandler UpdateCompleted;
        //
        // Summary:
        //     Occurs when ClickOnce has new status information for an update operation
        //     initiated by calling the System.Deployment.Application.ApplicationDeployment.UpdateAsync()
        //     method.
        public event DeploymentProgressChangedEventHandler UpdateProgressChanged;
#endif
#if Complete
        // Summary:
        //     Performs the same operation as System.Deployment.Application.ApplicationDeployment.CheckForUpdate(),
        //     but returns extended information about the available update.
        //
        // Returns:
        //     An System.Deployment.Application.UpdateCheckInfo for the available update.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     The current application is either not configured to support updates, or there
        //     is another update check operation already in progress.
        //
        //   System.Deployment.Application.DeploymentDownloadException:
        //     The deployment manifest cannot be downloaded. This exception will appear
        //     in the System.ComponentModel.AsyncCompletedEventArgs.Error property of the
        //     System.Deployment.Application.ApplicationDeployment.CheckForUpdateCompleted
        //     event.
        //
        //   System.Deployment.Application.InvalidDeploymentException:
        //     The deployment manifest is corrupted. Regenerate the application's manifest
        //     before you attempt to deploy this application to users. This exception will
        //     appear in the System.ComponentModel.AsyncCompletedEventArgs.Error property
        //     of the System.Deployment.Application.ApplicationDeployment.CheckForUpdateCompleted
        //     event.
        public UpdateCheckInfo CheckForDetailedUpdate();
        //
        // Summary:
        //     Performs the same operation as System.Deployment.Application.ApplicationDeployment.CheckForUpdate(),
        //     but returns extended information about the available update.
        //
        // Parameters:
        //   persistUpdateCheckResult:
        //     If false, the update will be applied silently and no dialog box will be displayed.
        //
        // Returns:
        //     An System.Deployment.Application.UpdateCheckInfo for the available update.
        public UpdateCheckInfo CheckForDetailedUpdate(bool persistUpdateCheckResult);
        //
        // Summary:
        //     Checks System.Deployment.Application.ApplicationDeployment.UpdateLocation
        //     to determine whether a new update is available.
        //
        // Returns:
        //     true if a new update is available; otherwise, false.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     ClickOnce throws this exception immediately if you call the System.Deployment.Application.ApplicationDeployment.CheckForUpdate()
        //     method while an update is already in progress.
        //
        //   System.Deployment.Application.DeploymentDownloadException:
        //     The deployment manifest cannot be downloaded.
        //
        //   System.Deployment.Application.InvalidDeploymentException:
        //     The deployment manifest is corrupted. You will likely need to redeploy the
        //     application to fix this problem.
        public bool CheckForUpdate();
        //
        // Summary:
        //     Checks System.Deployment.Application.ApplicationDeployment.UpdateLocation
        //     to determine whether a new update is available.
        //
        // Parameters:
        //   persistUpdateCheckResult:
        //     If false, the update will be applied silently and no dialog box will be displayed.
        //
        // Returns:
        //     true if a new update is available; otherwise, false.
        public bool CheckForUpdate(bool persistUpdateCheckResult);
        //
        // Summary:
        //     Checks System.Deployment.Application.ApplicationDeployment.UpdateLocation
        //     asynchronously to determine whether a new update is available.
        //
        // Exceptions:
        //   System.InvalidOperationException:
        //     ClickOnce throws this exception immediately if you call the System.Deployment.Application.ApplicationDeployment.CheckForUpdateAsync()
        //     method while an update is already in progress.
        //
        //   System.Deployment.Application.DeploymentDownloadException:
        //     The deployment manifest cannot be downloaded. This exception appears in the
        //     System.ComponentModel.AsyncCompletedEventArgs.Error property of the System.Deployment.Application.ApplicationDeployment.CheckForUpdateCompleted
        //     event.
        //
        //   System.Deployment.Application.InvalidDeploymentException:
        //     The deployment manifest is corrupted. You will likely need to redeploy the
        //     application to fix this problem. This exception appears in the System.ComponentModel.AsyncCompletedEventArgs.Error
        //     property of the System.Deployment.Application.ApplicationDeployment.CheckForUpdateCompleted
        //     event.
        public void CheckForUpdateAsync();
        //
        // Summary:
        //     Cancels the asynchronous update check.
        public void CheckForUpdateAsyncCancel();
        //
        // Summary:
        //     Downloads a set of optional files on demand.
        //
        // Parameters:
        //   groupName:
        //     The named group of files to download. All files marked "optional" in a ClickOnce
        //     application require a group name.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The groupName parameter is null or zero-length.
        public void DownloadFileGroup(string groupName);
        //
        // Summary:
        //     Downloads, on demand, a set of optional files in the background.
        //
        // Parameters:
        //   groupName:
        //     The named group of files to download. All files marked "optional" in a ClickOnce
        //     application require a group name.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The groupName parameter is null or zero-length.
        //
        //   System.InvalidOperationException:
        //     You cannot initiate more than one download of groupName at a time.
        public void DownloadFileGroupAsync(string groupName);
        //
        // Summary:
        //     Downloads, on demand, a set of optional files in the background, and passes
        //     a piece of application state to the event callbacks.
        //
        // Parameters:
        //   groupName:
        //     The named group of files to download. All files marked "optional" in a ClickOnce
        //     application require a group name.
        //
        //   userState:
        //     An arbitrary object containing state information for the asynchronous operation.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The groupName parameter is null or zero-length.
        //
        //   System.InvalidOperationException:
        //     You cannot initiate more than one download of groupName at a time.
        public void DownloadFileGroupAsync(string groupName, object userState);
        //
        // Summary:
        //     Cancels an asynchronous file download.
        //
        // Parameters:
        //   groupName:
        //     The named group of files to download. All files marked "optional" in a ClickOnce
        //     application require a group name.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     groupName cannot be null.
        public void DownloadFileGroupAsyncCancel(string groupName);
        //
        // Summary:
        //     Checks whether the named file group has already been downloaded to the client
        //     computer.
        //
        // Parameters:
        //   groupName:
        //     The named group of files to download. All files marked "optional" in a ClickOnce
        //     application require a group name.
        //
        // Returns:
        //     true if the file group has already been downloaded for the current version
        //     of this application; otherwise, false. If a new version of the application
        //     has been installed, and the new version has not added, removed, or altered
        //     files in the file group, System.Deployment.Application.ApplicationDeployment.IsFileGroupDownloaded(System.String)
        //     returns true.
        //
        // Exceptions:
        //   System.Deployment.Application.InvalidDeploymentException:
        //     groupName is not a file group defined in the application manifest.
        public bool IsFileGroupDownloaded(string groupName);
        //
        // Summary:
        //     Starts a synchronous download and installation of the latest version of this
        //     application.
        //
        // Returns:
        //     true if an application has been updated; otherwise, false.
        //
        // Exceptions:
        //   System.Deployment.Application.TrustNotGrantedException:
        //     The local computer did not grant the application the permission level it
        //     requested to execute.
        //
        //   System.Deployment.Application.InvalidDeploymentException:
        //     Your ClickOnce deployment is corrupted. For tips on how to diagnose and correct
        //     the problem, see Troubleshooting ClickOnce Deployments.
        //
        //   System.Deployment.Application.DeploymentDownloadException:
        //     The new deployment could not be downloaded from its location on the network.
        //
        //   System.InvalidOperationException:
        //     The application is currently being updated.
        public bool Update();
        //
        // Summary:
        //     Starts an asynchronous download and installation of the latest version of
        //     this application.
        //
        // Exceptions:
        //   System.Deployment.Application.TrustNotGrantedException:
        //     The local computer did not grant this application the permission level it
        //     requested to execute.
        //
        //   System.Deployment.Application.InvalidDeploymentException:
        //     Your ClickOnce deployment is corrupted. For tips on how to diagnose and correct
        //     the problem, see Troubleshooting ClickOnce Deployments.
        //
        //   System.Deployment.Application.DeploymentDownloadException:
        //     The new deployment could not be downloaded from its location on the network.
        public void UpdateAsync();
        //
        // Summary:
        //     Cancels an asynchronous update initiated by System.Deployment.Application.ApplicationDeployment.UpdateAsync().
        public void UpdateAsyncCancel();
#endif
    }
}