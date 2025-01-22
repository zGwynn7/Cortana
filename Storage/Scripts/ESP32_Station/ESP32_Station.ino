#include <WiFi.h>
#include <DHT.h>

const char WIFI_SSID[] = "Home&Life SuperWiFi-3451";
const char WIFI_PASSWORD[] = "3YRC8T4GB3X4A4XA";
const char CORTANA_IP[] = "192.168.1.117";
const int CORTANA_PORT = 5000;

const int led_blue = 14;
const int led_white = 13;
const int motion_big = 26;
const int motion_small = 25;
const int light_sensor = 33;
const int dht_sensor = 32;

DHT dht11(dht_sensor, DHT11);
WiFiClient client;

int currentMotionBig = LOW;
int currentMotionSmall = LOW;
int avgMotionBig = LOW;
int avgMotionSmall = LOW;
int lastMotionBig = LOW;
int lastMotionSmall = LOW;

unsigned long tcpTime;
const int transmissionTime = 2000;

void setup() {
  Serial.begin(9600);

  analogSetAttenuation(ADC_11db);
  dht11.begin();

  pinMode(motion_big, INPUT);
  pinMode(motion_small, INPUT);
  pinMode(led_blue, OUTPUT);
  pinMode(led_white, OUTPUT);

  Serial.print("Connecting to Wifi...");
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  while(WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.print("\nConnected to WiFi network with IP Address: ");
  Serial.println(WiFi.localIP());

  delay(500);
 
  Serial.print("Connecting to Cortana...");
  while (!client.connect(CORTANA_IP, CORTANA_PORT)) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("Connected to Cortana");
  client.print("esp32");

  tcpTime = millis();
}

void loop() {
  currentMotionBig = digitalRead(motion_big);
  currentMotionSmall = digitalRead(motion_small);

  if(currentMotionBig == HIGH) avgMotionBig = HIGH;
  if(currentMotionSmall == HIGH) avgMotionSmall = HIGH;

  if((millis() - tcpTime >= transmissionTime) || (avgMotionBig == HIGH && lastMotionBig == LOW) || (avgMotionSmall == HIGH && lastMotionSmall == LOW))
  {
    int hum  = (int) dht11.readHumidity();
    int temp = (int) dht11.readTemperature();
    int light = analogRead(light_sensor);
    
    char buff[100];
    snprintf(buff, 100, "{ \"bigMotion\": \"%s\", \"smallMotion\": \"%s\", \"light\": %d, \"temperature\": %d, \"humidity\": %d }", avgMotionBig == 1 ? "On" : "Off", avgMotionSmall == 1 ? "On" : "Off", light, temp, hum);
    client.print(buff);

    lastMotionBig = avgMotionBig;
    lastMotionSmall = avgMotionSmall;
    avgMotionBig = LOW;
    avgMotionSmall = LOW;

    delay(50);
    tcpTime = millis();
  }

  digitalWrite(led_blue, currentMotionBig);
  digitalWrite(led_white, currentMotionSmall);
  delay(50);
}