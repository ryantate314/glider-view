FROM ubuntu
# igcd will not run on alpine. Tried installing bash but no luck. ubuntu works.

ARG IGCD_VERSION=0.2.2

RUN  apt-get update \
  && apt-get install -y wget \
  && rm -rf /var/lib/apt/lists/*

WORKDIR /

RUN wget https://flightbook.glidernet.org/igcd/igcd-v${IGCD_VERSION}.tar.gz \
 && tar -xzf igcd-v${IGCD_VERSION}.tar.gz \
 && mv v${IGCD_VERSION} igcd

WORKDIR /igcd

# RUN chmod 777 igcd-linux-amd64

VOLUME ["/igcd/data"]

ENTRYPOINT ["/igcd/igcd-linux-amd64"]
