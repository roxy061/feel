#include <WiFi.h>
#include <PubSubClient.h>
#include <DHT.h>

const char* ssid = "IT Department";
const char* password = "media2022";
const char* mqtt_server = "192.168.11.180"; 

WiFiClient espClient;
PubSubClient client(espClient);

#define DHTPIN 4     
#define DHTTYPE DHT11   
DHT dht(DHTPIN, DHTTYPE);

unsigned long lastMsg = 0;
const long interval = 2000;

void setup_wifi() {
  delay(10);
  Serial.begin(115200);
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected");
}

void reconnect() {
  while (!client.connected()) {
    Serial.print("Attempting MQTT connection...");
    String clientId = "ESP32Client-DHT-";
    clientId += String(random(0xffff), HEX);
    
    if (client.connect(clientId.c_str())) {
      Serial.println("connected");
    } else {
      Serial.print("failed, rc=");
      Serial.print(client.state());
      Serial.println(" try again in 5s");
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  setup_wifi();
  client.setServer(mqtt_server, 1883);
  dht.begin();
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    WiFi.begin(ssid, password);
    while (WiFi.status() != WL_CONNECTED) {
      delay(500);
    }
  }

  if (!client.connected()) {
    reconnect();
  }
  
  client.loop();

  unsigned long now = millis();
  if (now - lastMsg > interval && client.connected()) {
    lastMsg = now;

    float h = dht.readHumidity();
    float t = dht.readTemperature();

    if (isnan(h) || isnan(t)) {
      Serial.println("DHT read failed!");
      return;
    }

    char hStr[10], tStr[10];
    snprintf(hStr, sizeof(hStr), "%.1f", h);
    snprintf(tStr, sizeof(tStr), "%.1f", t);

    client.publish("sensor/humidity", hStr);
    client.publish("sensor/temperature", tStr);

    Serial.print("H: "); Serial.print(hStr);
    Serial.print("% | T: "); Serial.print(tStr);
    Serial.println("C");
  }
}
