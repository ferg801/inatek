# ProGuard/R8 configuration for handling duplicate classes

# Allow duplicate classes to be merged instead of causing errors
-dontwarn java.lang.invoke.LambdaForm$Hidden

# Keep all AndroidX classes - CRITICAL for AppCompat
-keep class androidx.** { *; }
-keepnames class androidx.** { *; }
-keep class androidx.appcompat.** { *; }
-keep class androidx.appcompat.widget.** { *; }

# Keep specific AppCompat classes that are loaded via reflection
-keep class androidx.appcompat.widget.FitWindowsFrameLayout { *; }
-keep class androidx.appcompat.widget.ActionBarOverlayLayout { *; }
-keep class androidx.appcompat.widget.ContentFrameLayout { *; }

# Don't warn about missing dependencies
-dontwarn androidx.**
-dontwarn android.**
-dontwarn com.google.**

# Kotlin compatibility
-keepattributes *Annotation*
-keepattributes InnerClasses,EnclosingMethod

# Keep app's own code
-keepclasseswithmembernames class com.tenaris.inateckscanner.** {
    native <methods>;
}
