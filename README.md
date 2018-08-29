# VRToaster

Unity library to show toasts. Useful to show messages in VR.

## Usage

```// This toast will follow the right hand
Toast rightToast = Toaster.MakeToast(ToastGroup.RightHand, "default");
// This toast will follow the left hand
Toast leftToast = Toaster.MakeToast(ToastGroup.LeftHand, "default");
// This toast will follow the head rotation
Toast frontToast = Toaster.MakeToast(ToastGroup.Frontal, "default");

// Show, hide and destroy a toast
rightToast.Show("Hello world");
rightToast.Hide();
rightToast.Destroy();

// Show an error message for 5 seconds
Toaster.TimedToast(ToastGroup.Frontal, "An error occurred", 5, "error");```
