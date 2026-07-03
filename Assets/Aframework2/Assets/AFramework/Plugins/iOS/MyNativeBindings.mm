extern "C" {
    // Helper method to create C string copy
    char* MakeStringCopy (NSString* nsstring)
    {
        if (nsstring == NULL) {
            return NULL;
        }
        // convert from NSString to char with utf8 encoding
        const char* string = [nsstring cStringUsingEncoding:NSUTF8StringEncoding];
        if (string == NULL) {
            return NULL;
        }

        // create char copy with malloc and strcpy
        char* res = (char*)malloc(strlen(string) + 1);
        strcpy(res, string);
        return res;
    }
	
	char* cStringCopy(const char* string)
	{
		if (string == NULL)
		return NULL;
		char* res = (char*)malloc(strlen(string) + 1);
		strcpy(res, string);
		return res;
	}

    const char* GetSettingsURL () {
         NSURL * url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
         return MakeStringCopy(url.absoluteString);
    }

    void OpenSettings () {
        NSURL * url = [NSURL URLWithString: UIApplicationOpenSettingsURLString];
        [[UIApplication sharedApplication] openURL: url];
    }
	
	char* IOSgetPhoneCountryCode()
	{
		NSLocale *locale = [NSLocale currentLocale];
		NSString *countryCode = [locale objectForKey: NSLocaleCountryCode];
		//NSString *countryName = [locale displayNameForKey: NSLocaleCountryCode value: countryCode];
		return cStringCopy([countryCode UTF8String]);
	}
}