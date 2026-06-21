#include <arpa/inet.h>
#include <netinet/in.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/socket.h>
#include <sys/wait.h>
#include <unistd.h>
#include <string.h>

char SERVER_ADDRESS[16] = { 0 };
short SERVER_PORT = NULL;
char ID[6] = { 0 };
char KEY[16] = { 0 };

#define BUFSIZE        512

typedef unsigned char byte;

void set_end_point(struct sockaddr_in*, sa_family_t, const char*, unsigned short);
void send_until_suc(int, short, const char*);
int run_ffmpeg();

int main() {
    printf("來自 %s 的問候!\n", "Pi");

    FILE* config_file = fopen("/home/pi/config.txt", "r");
    char file_buf[32] = { 0 };

    if (config_file == NULL)
        printf("Not able to open the file.");

    fscanf(config_file, "%s", SERVER_ADDRESS);
    fscanf(config_file, "%s", file_buf);
    fscanf(config_file, "%s", ID);
    fscanf(config_file, "%s", KEY);

    puts(SERVER_ADDRESS);
    puts(file_buf);
    puts(ID);
    puts(KEY);

    SERVER_PORT = atoi(file_buf);


    struct sockaddr_in addr = { };
    set_end_point(&addr, AF_INET, SERVER_ADDRESS, SERVER_PORT);

    int sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == -1) {
        perror("Socket creation error");
        return EXIT_FAILURE;
    }

    while (connect(sock, (struct sockaddr *) &addr, sizeof(addr)) == -1)
        perror("Connection error\n");

    send_until_suc(sock, 0, ID);
    send_until_suc(sock, 1, KEY);

    byte buf[BUFSIZE] { };
    ssize_t readden;
    while ((readden = recv(sock, buf, BUFSIZE, 0)) < 0)
        perror("Receive error, try again...");

    if (readden == 0) {
        fprintf(stderr, "Client orderly shut down the connection.\n");
        return EXIT_FAILURE;
    }
    // readden > 0
    while (readden != 3) {
        fprintf(stderr, "Recv len = %d", readden);
        byte tmp[256] = { };
        int r = recv(sock, tmp, 256, 0);
        if (r < 0) continue;
        for (int i = 0; i < r; i++)
            buf[readden + i] = tmp[i];
        readden += r;
    }
    write(STDOUT_FILENO, buf, readden);

    if (strcmp((char *) buf, "suc")) return EXIT_FAILURE;

    close(sock);

    // if buf == "suc":
    while (true) {
        run_ffmpeg();
    }

    return EXIT_SUCCESS;
}



void set_end_point(struct sockaddr_in* addr, sa_family_t family, const char* ip, unsigned short port) {
    addr->sin_family = family;
    addr->sin_port = htons(port); /*converts short to short with network byte order*/
    addr->sin_addr.s_addr = inet_addr(ip);
}

// Cancat the flag and masage, return the total len.
int concat(short flag, const char* msg, char buf[]) {
    int len = 1;
    buf[0] = flag;
    for (; len <= strlen(msg); len++)
        buf[len] = msg[len - 1];
    return len;
}

void send_until_suc(int sock, short flag, const char* msg) {
    char buf[BUFSIZE / 2] { };
    int len = concat(flag, msg, buf);
    while (send(sock, buf, len, 0) == -1)
        perror("Send error, try again...");
}

int run_ffmpeg() {
    pid_t pid = fork();
    char addr[50] = "rtmp://";
    strcat(addr, SERVER_ADDRESS);
    strcat(addr, "/live/");
    puts(addr);
    size_t addr_len = strlen(addr);
    for (int i = addr_len; i < addr_len + 15; i++)
        addr[i] = KEY[i - addr_len];
    addr[addr_len + 15] = 0;
    printf(addr);
    if (pid == 0) {   // Sub Process
        char *const argv[] = {
            "ffmpeg",
            "-input_format", "h264",
            "-f", "video4linux2",
            "-s", "1280x720",
            "-r", "24",
            "-i", "/dev/video0",
            "-c:v", "copy",
            //"-r",  "25",
            "-b:v", "1M",
            "-an",
            "-max_delay", "10",
            "-g", "6",
            "-threads", "2",
            "-f", "flv",
            addr, NULL
        };
        if (execvp("ffmpeg", argv) < 0)
            fprintf(stderr, "FFmpeg run failed...");
    }
    return wait(&pid);
}
